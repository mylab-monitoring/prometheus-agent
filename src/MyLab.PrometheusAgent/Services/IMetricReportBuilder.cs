﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLab.PrometheusAgent.Tools;

namespace MyLab.PrometheusAgent.Services
{
    public interface IMetricReportBuilder
    {
        Task<IEnumerable<MetricModel>> Build();
    }

    class MetricReportBuilder : IMetricReportBuilder
    {
        private readonly ITargetsMetricProvider _targetsMetricProvider;
        private readonly IScrapeConfigProvider _scrapeConfigProvider;

        public MetricReportBuilder(
            ITargetsMetricProvider targetsMetricProvider,
            IScrapeConfigProvider scrapeConfigProvider)
        {
            _targetsMetricProvider = targetsMetricProvider;
            _scrapeConfigProvider = scrapeConfigProvider;
        }

        public async Task<IEnumerable<MetricModel>> Build()
        {
            var targetMetrics = await _targetsMetricProvider.Provide();
            var config = await _scrapeConfigProvider.Provide();
                
            return config.Items
                .SelectMany(itm => itm.StaticConfigs
                    .SelectMany(c => StaticConfigToReportItem(targetMetrics, c, itm.JobName)));
        }

        IEnumerable<MetricModel> StaticConfigToReportItem(TargetMetrics[] targetMetrics, ScrapeStaticConfig cfg, string jobName)
        {
            var configJoinedMetrics = targetMetrics
                .Where(metricTarget => cfg.Targets.Contains(metricTarget.Id))
                .ToArray();

            var resMetrics = configJoinedMetrics
                .SelectMany(metricTarget => metricTarget.Metrics == null 
                    ? Enumerable.Empty<MetricModel>()
                    : metricTarget.Metrics.Select(m => m
                        .AddLabels(cfg.Labels)
                        .AddLabels(new Dictionary<string, string>
                        {
                            {"instance", metricTarget.Id},
                            {"job", jobName}
                        })
                    ))
                .ToArray();

            return resMetrics;
        }
    }
}