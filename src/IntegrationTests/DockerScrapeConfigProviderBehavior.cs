using System;
using System.Linq;
using System.Threading.Tasks;
using MartinCostello.Logging.XUnit;
using MyLab.Log.Dsl;
using MyLab.PrometheusAgent;
using MyLab.PrometheusAgent.Model;
using MyLab.PrometheusAgent.Tools;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class DockerScrapeConfigProviderBehavior
    {
        private readonly ITestOutputHelper _output;
        private readonly IDslLogger _logger;

        public DockerScrapeConfigProviderBehavior(ITestOutputHelper output)
        {
            _output = output;
            var logger = new XUnitLogger("", output, new XUnitLoggerOptions());
            _logger = logger.Dsl();
        }

        DockerScrapeSourceProvider CreateProvider(DockerDiscoveryStrategy discoveryStrategy)
        {
            return new DockerScrapeSourceProvider("npipe://./pipe/docker_engine", discoveryStrategy)
            {
                Log = _logger
            };
        }

        [Theory]
        [InlineData("http://prometheus-agent-target-autocfg-1:80/metrics")]
        [InlineData("http://prometheus-agent-target-autocfg-2:12345/metrics")]
        [InlineData("http://prometheus-agent-target-autocfg-3:12345/metrics")]
        [InlineData("http://prometheus-agent-target-autocfg-4:80/foo")]
        public async Task ShouldDetectTarget(string expectedUrl)
        {
            //Arrange
            var provider = CreateProvider(DockerDiscoveryStrategy.All);

            //Act
            var selectedSources = await LoadTestContainers(provider);
            var staticCfg = selectedSources.SingleOrDefault(c => c.ScrapeUrl.OriginalString == expectedUrl);

            //Assert
            Assert.NotNull(staticCfg);
        }

        [Theory]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-1")]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-2")]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-3")]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-4")]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-6")]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-7")]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-8")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-7")]
        public async Task ShouldIncludeByLabelAndDiscoveryStrategy(DockerDiscoveryStrategy strategy, string shouldBeFound)
        {
            //Arrange
            var provider = CreateProvider(strategy);

            //Act
            var selectedSources = await LoadTestContainers(provider);
            var staticCfg = selectedSources.SingleOrDefault(c => c.ScrapeUrl.Host == shouldBeFound);

            //Assert
            Assert.NotNull(staticCfg);
        }

        [Theory]
        [InlineData(DockerDiscoveryStrategy.All, "prometheus-agent-target-autocfg-5")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-1")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-2")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-3")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-4")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-5")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-6")]
        [InlineData(DockerDiscoveryStrategy.Include, "prometheus-agent-target-autocfg-8")]
        public async Task ShouldExcludeByLabelAndDiscoveryStrategy(DockerDiscoveryStrategy strategy, string shouldNotBeFound)
        {
            //Arrange
            var provider = CreateProvider(strategy);

            //Act
            var selectedSources = await LoadTestContainers(provider);
            var staticCfg = selectedSources.FirstOrDefault(c => c.ScrapeUrl.Host == shouldNotBeFound);

            //Assert
            Assert.Null(staticCfg);
        }

        [Fact]
        public async Task ShouldProvideRegularContainerLabels()
        {
            //Arrange
            var provider = CreateProvider(DockerDiscoveryStrategy.All);

            //Act
            var selectedSources = await LoadTestContainers(provider);

            var staticCfg = selectedSources.FirstOrDefault(c => c.ScrapeUrl.Host == "prometheus-agent-target-autocfg-1");

            staticCfg.Labels.TryGetValue("container_label_foo", out var labelValue);

            //Assert
            Assert.Equal("label_foo", labelValue);
        }

        [Fact]
        public async Task ShouldProvideAsIsContainerLabels()
        {
            //Arrange
            var provider = CreateProvider(DockerDiscoveryStrategy.All);

            //Act
            var selectedSources = await LoadTestContainers(provider);

            var staticCfg = selectedSources.FirstOrDefault(c => c.ScrapeUrl.Host == "prometheus-agent-target-autocfg-1");

            staticCfg.Labels.TryGetValue("bar", out var labelValue);

            //Assert
            Assert.Equal("label_bar", labelValue);
        }

        private static async Task<ScrapeSourceDescription[]> LoadTestContainers(DockerScrapeSourceProvider provider)
        {
            var providedSources = await provider.LoadAsync();
            ScrapeSourceDescription[] selectedSources;

            try
            {
                selectedSources = providedSources
                    .Where(s => s.ScrapeUrl.Host.Contains("prometheus-agent-target-autocfg"))
                    .ToArray();
            }
            finally
            {
                provider.Dispose();
            }

            return selectedSources;
        }
    }
}
