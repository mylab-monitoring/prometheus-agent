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
        private readonly IMetricReportBuilder _metricReportBuilder;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(IMetricReportBuilder metricReportBuilder, ILogger<MetricsController> logger)
        {
            _metricReportBuilder = metricReportBuilder;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var report = await _metricReportBuilder.Build();

            var resultBuilder = new StringBuilder();
            var resultWriter = new StringWriter(resultBuilder);

            foreach (var metric in report)
            {
                await metric.WriteAsync(resultWriter);
            }

            return Ok(resultBuilder.ToString());
        }
    }
}
