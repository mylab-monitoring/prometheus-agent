using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.PrometheusAgent.Tools;

namespace MyLab.PrometheusAgent.Services
{
    public interface IScrapeConfigService
    {
        Task<ScrapeConfig> Provide();
    }

    public class ScrapeConfigService : IScrapeConfigService
    {
        private readonly IScrapeConfigProvider _configProvider;

        public ScrapeConfigService(IOptions<PrometheusAgentOptions> options, ILogger<ScrapeConfigService> logger = null)
            : this(options.Value, logger)
        {

        }

        public ScrapeConfigService(PrometheusAgentOptions options, ILogger<ScrapeConfigService> logger = null)
        {
            var configProviders = new List<IScrapeConfigProvider>();

            if (options.Config != null)
                configProviders.Add(new FileScrapeConfigProvider(options.Config));

            if (options.Docker.Strategy != DockerDiscoveryStrategy.None)
                configProviders.Add(new DockerScrapeConfigProvider(options.Docker.Socket, options.Docker.Strategy)
                {
                    Log = logger.Dsl(),
                    AdditionalLabels = options.Docker.Labels
                });

            TimeSpan? cfgExpiry = options.ConfigExpirySec == 0
                ? (TimeSpan?) null
                : TimeSpan.FromSeconds(options.ConfigExpirySec);

            _configProvider = new ScrapeConfigProviderAggregator(configProviders, cfgExpiry);
        }

        public Task<ScrapeConfig> Provide()
        {
            return _configProvider.LoadAsync();
        }
    }
}