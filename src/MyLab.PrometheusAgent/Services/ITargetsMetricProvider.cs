using System.Collections.Concurrent;
using System.Threading.Tasks;
using MyLab.PrometheusAgent.Model;
using MyLab.PrometheusAgent.Tools;

namespace MyLab.PrometheusAgent.Services
{
    public interface ITargetsMetricProvider
    {
        Task<MetricReport> ProvideAsync();
    }
}
