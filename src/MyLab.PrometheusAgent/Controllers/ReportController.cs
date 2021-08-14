using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLab.PrometheusAgent.Services;
using Newtonsoft.Json;

namespace MyLab.PrometheusAgent.Controllers
{
    [ApiController]
    [Route("report")]
    public class ReportController : ControllerBase
    {
        private readonly TargetsReportService _targetsReportService;
        private readonly ILogger<MetricsController> _logger;

        public ReportController(TargetsReportService targetsReportService, ILogger<MetricsController> logger)
        {
            _targetsReportService = targetsReportService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var report = _targetsReportService.Items.Values.ToArray();

            var json = JsonConvert.SerializeObject(report, Formatting.Indented);

            return base.Content(json, "application/json", Encoding.UTF8);
        }
    }
}
