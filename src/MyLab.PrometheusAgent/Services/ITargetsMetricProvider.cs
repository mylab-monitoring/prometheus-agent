using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private readonly IScrapeConfigProvider _scrapeConfigProvider;
        private MetricTargetsReferences _uniqueMetricTargets;
        private readonly IDslLogger _logger;

        public TargetsMetricProvider(IScrapeConfigProvider scrapeConfigProvider, ILogger<TargetsMetricProvider> logger)
        {
            _scrapeConfigProvider = scrapeConfigProvider;
            _logger = logger.Dsl();
        }

        public async Task<TargetMetrics[]> Provide()
        {
            if (_uniqueMetricTargets == null)
            {
                var scrapeConfig = await _scrapeConfigProvider.Provide();
                _uniqueMetricTargets = MetricTargetsReferences.LoadUniqueScrapeConfig(scrapeConfig);
            }

            var targetMetrics = new ConcurrentBag<TargetMetrics>();
            
            var loadTasks = _uniqueMetricTargets
                .Select(async r =>
                {
                    try
                    {
                        var m= await RequestMetrics(r);

                        if(m != null)
                            targetMetrics.Add(m);
                    }
                    catch (Exception e)
                    { 
                        _logger.Error(e)
                            .AndFactIs("target", r.Id)
                            .Write();
                    }
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

        private async Task<TargetMetrics> RequestMetrics(MetricTargetReference arg)
        {
            HttpResponseMessage response;

            try
            {
                response = await arg.Client.GetAsync("metrics");
            }
            catch (HttpRequestException e) when (e.Message.StartsWith("Name or service not known"))
            {
                _logger.Warning(e).AndFactIs("target", arg.Id);

                return null;
            }

            var result = new TargetMetrics
            {
                Id = arg.Id
            };

            if (response.IsSuccessStatusCode)
            {
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
                _logger.Warning("Metric target return bad response")
                    .AndFactIs("http-code", $"{(int) response.StatusCode}({response.ReasonPhrase})")
                    .AndFactIs("target", arg.Id);
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
