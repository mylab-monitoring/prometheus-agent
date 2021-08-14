using System.Threading.Tasks;
using MyLab.PrometheusAgent.Model;

namespace MyLab.PrometheusAgent.Tools
{
    interface IScrapeSourceProvider
    {
        public Task<ScrapeSourceDescription[]> LoadAsync();
    }
}
