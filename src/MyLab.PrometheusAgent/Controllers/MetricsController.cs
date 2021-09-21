using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLab.PrometheusAgent.Services;

namespace MyLab.PrometheusAgent.Controllers
{
    [ApiController]
    [Route("metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly ITargetsMetricProvider _targetsMetricProvider;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(
            ITargetsMetricProvider targetsMetricProvider,
            ILogger<MetricsController> logger)
        {
            _targetsMetricProvider = targetsMetricProvider;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var metricsReport = await _targetsMetricProvider.ProvideAsync();
            
            var resultBuilder = new StringBuilder();
            var resultWriter = new StringWriter(resultBuilder);

            await metricsReport.WriteAsync(resultWriter);
            
            return Ok(resultBuilder.ToString());
        }
    }
}
