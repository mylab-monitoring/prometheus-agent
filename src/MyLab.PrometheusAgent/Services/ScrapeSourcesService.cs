using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.PrometheusAgent.Model;
using MyLab.PrometheusAgent.Tools;

namespace MyLab.PrometheusAgent.Services
{
    public class ScrapeSourcesService : IScrapeSourcesService
    {
        private readonly List<IScrapeSourceProvider> _configProviders;

        public ScrapeSourcesService(IOptions<PrometheusAgentOptions> options, ILogger<ScrapeSourcesService> logger = null)
            : this(options.Value, logger)
        {

        }

        public ScrapeSourcesService(PrometheusAgentOptions options, ILogger<ScrapeSourcesService> logger = null)
        {
            _configProviders = new List<IScrapeSourceProvider>();

            if (options.ScrapeConfig != null)
                _configProviders.Add(new FileScrapeSourceProvider(options.ScrapeConfig));

            if (options.Docker.Strategy != DockerDiscoveryStrategy.None)
                _configProviders.Add(new DockerScrapeSourceProvider(options.Docker.Socket, options.Docker.Strategy)
                {
                    Log = logger.Dsl(),
                    AdditionalLabels = options.Docker.Labels,
                    ExcludeServiceLabels = options.Docker.DisableServiceContainerLabels
                });
        }

        public async Task<ScrapeSourceDescription[]> ProvideAsync()
        {
            var results = new List<ScrapeSourceDescription>();

            foreach (var scrapeSourceProvider in _configProviders)
            {
                var scrapeSources = await scrapeSourceProvider.LoadAsync();

                if(scrapeSources != null)
                    results.AddRange(scrapeSources);
            }

            return results.ToArray();
        }
    }
}