using System;
using System.Linq;
using System.Threading.Tasks;
using MartinCostello.Logging.XUnit;
using MyLab.Log.Dsl;
using MyLab.PrometheusAgent;
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

        DockerScrapeConfigProvider CreateProvider(DockerDiscoveryStrategy discoveryStrategy)
        {
            return new DockerScrapeConfigProvider("npipe://./pipe/docker_engine", discoveryStrategy)
            {
                Log = _logger
            };
        }

        [Theory]
        [InlineData("prometheus-agent-target-autocfg-1")]
        //[InlineData("http://prometheus-agent-target-autocfg-1:80/metrics")]
        //[InlineData("http://prometheus-agent-target-autocfg-2:80/metrics")]
        //[InlineData("http://prometheus-agent-target-autocfg-3:12345/metrics")]
        //[InlineData("http://prometheus-agent-target-autocfg-4:80/foo")]
        public async Task ShouldDetectTarget(string expectedUrl)
        {
            //Arrange
            var provider = CreateProvider(DockerDiscoveryStrategy.All);

            //Act
            var cfg = await provider.LoadAsync();
            ScrapeStaticConfig[] targets;

            try
            {
                targets = cfg.Jobs.FirstOrDefault()?.StaticConfigs
                    .Where(c => c.Targets.Any(n => n.Contains("prometheus-agent-target-autocfg")))
                    .ToArray();
            }
            finally
            {
                provider.Dispose();
            }

            var staticCfg = targets?.SingleOrDefault(c => c.Targets.Contains(expectedUrl));

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
            var cfg = await provider.LoadAsync();
            ScrapeStaticConfig[] targets;

            try
            {
                targets = cfg.Jobs.FirstOrDefault()?.StaticConfigs
                    .Where(c => c.Targets.Any(n => n.Contains("prometheus-agent-target-autocfg")))
                    .ToArray();
            }
            finally
            {
                provider.Dispose();
            }

            var staticCfg = targets?.SingleOrDefault(c => c.Targets.Any(t => t.Contains(shouldBeFound)));

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
            var cfg = await provider.LoadAsync();
            ScrapeStaticConfig[] targets;

            try
            {
                targets = cfg.Jobs.FirstOrDefault()?.StaticConfigs
                    .Where(c => c.Targets.Any(n => n.Contains("prometheus-agent-target-autocfg")))
                    .ToArray();
            }
            finally
            {
                provider.Dispose();
            }

            var staticCfg = targets?.SingleOrDefault(c => c.Targets.Any(t => t.Contains(shouldNotBeFound)));

            //Assert
            Assert.Null(staticCfg);
        }

        [Fact]
        public async Task ShouldProvideContainerLabels()
        {
            //Arrange
            var provider = CreateProvider(DockerDiscoveryStrategy.All);

            //Act
            var cfg = await provider.LoadAsync();
            ScrapeStaticConfig[] targets;

            try
            {
                targets = cfg.Jobs.FirstOrDefault()?.StaticConfigs
                    .Where(c => c.Targets.Any(n => n.Contains("prometheus-agent-target-autocfg")))
                    .ToArray();
            }
            finally
            {
                provider.Dispose();
            }

            var staticCfg = targets?.SingleOrDefault(c => c.Targets.Any(t => t.Contains("prometheus-agent-target-autocfg-1")));

            staticCfg.Labels.TryGetValue("label_foo", out var labelValue);

            //Assert
            Assert.Equal("container_label_label_bar", labelValue);
        }
    }
}
