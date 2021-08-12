using System.Threading.Tasks;

namespace MyLab.PrometheusAgent.Tools
{
    interface IScrapeConfigProvider
    {
        public Task<ScrapeConfig> LoadAsync();
    }
}
