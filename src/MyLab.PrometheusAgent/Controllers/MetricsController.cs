using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MyLab.PrometheusAgent.Controllers
{
    [ApiController]
    [Route("metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(ILogger<MetricsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
