using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MyLab.PrometheusAgent.Tools;

namespace MyLab.PrometheusAgent.Services
{
    public interface IScrapeConfigProvider
    {
        Task<ScrapeConfig> Provide();
    }

    public class ScrapeConfigProvider : IScrapeConfigProvider
    {
        private readonly PrometheusAgentOptions _options;
        private ScrapeConfig _scrapeConfig;

        public ScrapeConfigProvider(IOptions<PrometheusAgentOptions> options)
            : this(options.Value)
        {

        }

        public ScrapeConfigProvider(PrometheusAgentOptions options)
        {
            _options = options;
        }

        public async Task<ScrapeConfig> Provide()
        {
            if(string.IsNullOrEmpty(_options.Config))
                throw new InvalidOperationException("Scrape config path is not configured");
            if (!File.Exists(_options.Config))
                throw new InvalidOperationException("Scrape config path does not exists");

            return _scrapeConfig ??= ScrapeConfig.Parse(await File.ReadAllTextAsync(_options.Config));
        }
    }
}