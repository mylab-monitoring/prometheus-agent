using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLab.PrometheusAgent.Services;
using Newtonsoft.Json;

namespace MyLab.PrometheusAgent.Controllers
{
    [ApiController]
    [Route("config")]
    public class ConfigController : ControllerBase
    {
        private readonly IScrapeConfigService _scrapeConfigService;
        private readonly ILogger<MetricsController> _logger;

        public ConfigController(IScrapeConfigService scrapeConfigService, ILogger<MetricsController> logger)
        {
            _scrapeConfigService = scrapeConfigService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var scrapeConfig = await _scrapeConfigService.Provide();

            var json = JsonConvert.SerializeObject(scrapeConfig);

            return base.Content(json, "application/json", Encoding.UTF8);
        }
    }
}
