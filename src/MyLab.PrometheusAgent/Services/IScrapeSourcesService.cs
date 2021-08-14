using System;
using System.Linq;
using System.Threading.Tasks;
using MyLab.PrometheusAgent.Model;

namespace MyLab.PrometheusAgent.Services
{
    public interface IScrapeSourcesService
    {
        Task<ScrapeSourceDescription[]> ProvideAsync();
    }
}