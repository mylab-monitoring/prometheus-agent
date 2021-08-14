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
        private readonly IScrapeSourcesService _scrapeSourcesService;
        private readonly ILogger<MetricsController> _logger;

        public ConfigController(IScrapeSourcesService scrapeSourcesService, ILogger<MetricsController> logger)
        {
            _scrapeSourcesService = scrapeSourcesService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var scrapeSources = await _scrapeSourcesService.ProvideAsync();

            var json = JsonConvert.SerializeObject(scrapeSources);

            return base.Content(json, "application/json", Encoding.UTF8);
        }
    }
}
