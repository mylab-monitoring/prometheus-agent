using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.Log;
using MyLab.PrometheusAgent.Tools;

namespace MyLab.PrometheusAgent.Services
{
    public interface ITargetsMetricProvider
    {
        Task<TargetMetrics[]> Provide();
    }

    class TargetsMetricProvider : ITargetsMetricProvider
    {
        private readonly PrometheusAgentOptions _options;
        private readonly IScrapeConfigService _scrapeConfigService;
        private readonly TargetsReportService _targetsReportService;
        private MetricTargetsReferences _uniqueMetricTargets;
        private readonly IDslLogger _logger;

        public TargetsMetricProvider(
            IOptions<PrometheusAgentOptions> options,
            IScrapeConfigService scrapeConfigService,
            TargetsReportService targetsReportService,
            ILogger<TargetsMetricProvider> logger)
            :this(options.Value, scrapeConfigService, targetsReportService, logger)
        {
        }

        public TargetsMetricProvider(
            PrometheusAgentOptions options,
            IScrapeConfigService scrapeConfigService, 
            TargetsReportService targetsReportService,
            ILogger<TargetsMetricProvider> logger)
        {
            _options = options;
            _scrapeConfigService = scrapeConfigService;
            _targetsReportService = targetsReportService;
            _logger = logger.Dsl();
        }

        public async Task<TargetMetrics[]> Provide()
        {
            if (_uniqueMetricTargets == null)
            {
                var scrapeConfig = await _scrapeConfigService.Provide();

                var timeout = TimeSpan.FromSeconds(
                    _options.ScrapeTimeoutSec != 0
                        ? _options.ScrapeTimeoutSec
                        : 5
                );
                _uniqueMetricTargets = MetricTargetsReferences.LoadUniqueScrapeConfig(scrapeConfig, timeout);
            }

            var targetMetrics = new ConcurrentBag<TargetMetrics>();
            
            var loadTasks = _uniqueMetricTargets
                .Select(async r =>
                {
                    var reportItem = new TargetsReportItem
                    {
                        Id = r.Id,
                        Dt = DateTime.Now
                    };

                    try
                    {
                        var m = await RequestMetrics(r, reportItem);

                        if (m != null)
                        {
                            reportItem.MetricsCount = m.Metrics?.Length ?? 0;
                            targetMetrics.Add(m);
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        reportItem.Error = ExceptionDto.Create(httpEx);

                        _logger.Warning("Cant get metrics")
                            .AndFactIs("target", r.Id)
                            .AndFactIs("error-msg", httpEx.Message)
                            .Write();
                    }
                    catch (Exception e)
                    { 
                        reportItem.Error = ExceptionDto.Create(e);

                        _logger.Error(e)
                            .AndFactIs("target", r.Id)
                            .Write();
                    }

                    _targetsReportService.Report(reportItem);
                })
                .ToArray();

            try
            {
                Task.WaitAll(loadTasks, TimeSpan.FromSeconds(20));
            }
            catch (TimeoutException e)
            {
                _logger.Error("Can't scrape targets: timeout", e)
                    .Write();
            }

            return targetMetrics.ToArray();
        }

        private async Task<TargetMetrics> RequestMetrics(MetricTargetReference arg, TargetsReportItem reportItem)
        {
            HttpResponseMessage response;

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                response = await arg.Client.GetAsync("metrics");
                reportItem.ResponseVolume = response.Content.Headers.ContentLength.GetValueOrDefault(-1);
            }
            finally
            {
                sw.Stop();
                reportItem.Duration = sw.Elapsed;
            }

            var result = new TargetMetrics
            {
                Id = arg.Id
            };

            if (response.IsSuccessStatusCode)
            {
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType != null && mediaType != "text/plain")
                {
                    throw new InvalidOperationException("Metrics response content-type specified and not supported")
                        .AndFactIs("content-type", mediaType);
                }

                try
                {
                    var stringContent= await response.Content.ReadAsStringAsync();
                    var reader = new StringReader(stringContent);

                    var metrics = new List<MetricModel>();

                    while (reader.Peek() != -1)
                    {
                        metrics.Add(await MetricModel.ReadAsync(reader));
                    }

                    result.Metrics = metrics.ToArray();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Metrics parsing error", e);
                }
                
            }
            else
            {
                _logger.Warning("Metric target return bad response code")
                    .AndFactIs("http-code", $"{(int) response.StatusCode}({response.ReasonPhrase})")
                    .Write();
            }

            return result;
        }
    }

    public class TargetMetrics
    {
        public string Id { get; set; }
        public MetricModel[] Metrics { get; set; }
    }
}
