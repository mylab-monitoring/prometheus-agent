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
            return _scrapeConfig ??= ScrapeConfig.Parse(await File.ReadAllTextAsync(_options.ScrapeConfigPath));
        }
    }
}