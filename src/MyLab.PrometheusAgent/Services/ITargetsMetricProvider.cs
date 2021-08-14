using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MyLab.PrometheusAgent.Services
{
    public interface ITargetsMetricProvider
    {
        Task<MetricModel[]> ProvideAsync();
    }
}
