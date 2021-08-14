using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.PrometheusAgent.Model;

namespace MyLab.PrometheusAgent.Services
{
    class TargetsMetricProvider : ITargetsMetricProvider, IDisposable
    {
        private readonly IScrapeSourcesService _scrapeSourcesService;
        private readonly TargetsReportService _targetsReportService;
        private readonly IDslLogger _logger;

        private MetricSource[] _metricSources;
        private DateTime _metricsActualDt;

        private readonly TimeSpan _scrapeTimeout;
        private readonly TimeSpan _surveyTimeout;
        private readonly TimeSpan _configExpiry;

        public TargetsMetricProvider(
            IOptions<PrometheusAgentOptions> options,
            IScrapeSourcesService scrapeSourcesService,
            TargetsReportService targetsReportService,
            ILogger<TargetsMetricProvider> logger)
            :this(options.Value, scrapeSourcesService, targetsReportService, logger)
        {
        }

        public TargetsMetricProvider(
            PrometheusAgentOptions options,
            IScrapeSourcesService scrapeSourcesService, 
            TargetsReportService targetsReportService,
            ILogger<TargetsMetricProvider> logger)
        {
            _scrapeSourcesService = scrapeSourcesService;
            _targetsReportService = targetsReportService;
            _logger = logger.Dsl();

            _scrapeTimeout = TimeSpan.FromSeconds(options.ScrapeTimeoutSec ?? 10);
            _surveyTimeout = _scrapeTimeout.Add(TimeSpan.FromSeconds(1));
            _configExpiry = options.ConfigExpirySec.HasValue
                ? TimeSpan.FromSeconds(options.ConfigExpirySec.Value)
                : TimeSpan.FromMinutes(1);
        }

        public async Task<MetricModel[]> ProvideAsync()
        {
            await InitSourcesAsync();

            var resultMetrics = new List<MetricModel>();
            var resultMetricSync = new object();
            
            var loadTasks = _metricSources
                .Select(async s =>
                {
                    var scrapingResult = await s.ScrapeMetricsAsync();

                    lock (resultMetricSync)
                    {
                        if (scrapingResult.Metrics != null)
                            resultMetrics.AddRange(scrapingResult.Metrics);

                        _targetsReportService.Report(scrapingResult.Report);
                    }
                })
                .ToArray();
            
            try
            {
                Task.WaitAll(loadTasks, _surveyTimeout);
            }
            catch (TimeoutException)
            {
                _logger.Error("Can't scrape targets: timeout")
                    .AndFactIs("scrape-timeout", _scrapeTimeout)
                    .AndFactIs("survey-timeout", _surveyTimeout)
                    .Write();
            }

            return resultMetrics.ToArray();
        }

        private async Task InitSourcesAsync()
        {
            if (_metricSources != null)
            {
                var sourceListTooOld = (DateTime.Now - _metricsActualDt) > _configExpiry;

                if(!sourceListTooOld)
                    return;
            }

            var sourceDescriptions = await _scrapeSourcesService.ProvideAsync().ContinueWith(t => t.Result);
            _metricSources = sourceDescriptions
                .Select(d => new MetricSource(d.ScrapeUrl, _scrapeTimeout, d.Labels)
                {
                    Log = _logger
                })
                .ToArray();
            _metricsActualDt = DateTime.Now;
        }

        public void Dispose()
        {
            if (_metricSources != null)
            {
                foreach (var metricSource in _metricSources)
                {
                    metricSource.Dispose();
                }
            }
        }
    }
}