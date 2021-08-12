using System;
using System.IO;
using System.Threading.Tasks;
using MyLab.PrometheusAgent.Tools;
using Xunit;

namespace UnitTests
{
    public class ScrapeConfigBehavior
    {
        [Fact]
        public async Task ShouldParse()
        {
            //Arrange
            var yaml = await File.ReadAllTextAsync("config.yml");

            //Act
            var config = ScrapeConfig.Parse(yaml);

            //Assert
            Assert.NotNull(config);
            Assert.NotNull(config.Jobs);
            Assert.Equal(2, config.Jobs.Length);

            var item1 = config.Jobs[0];

            Assert.Equal("env-metrics", item1.JobName);
            Assert.NotNull(item1.StaticConfigs);
            Assert.Equal(2, item1.StaticConfigs.Length);

            var item1Config1 = item1.StaticConfigs[0];
            Assert.NotNull(item1Config1.Targets);
            Assert.Equal(2, item1Config1.Targets.Length);
            Assert.Equal("cadvisor:8080", item1Config1.Targets[0]);
            Assert.Equal("nodeexporter:9100", item1Config1.Targets[1]);
            Assert.Single(item1Config1.Labels);
            Assert.True(item1Config1.Labels.ContainsKey("host"));
            Assert.Equal("prod-service", item1Config1.Labels["host"]);

            var item1Config2 = item1.StaticConfigs[1];
            Assert.NotNull(item1Config2.Targets);
            Assert.Equal(2, item1Config2.Targets.Length);
            Assert.Equal("192.168.80.203:7301", item1Config2.Targets[0]);
            Assert.Equal("192.168.80.203:7302", item1Config2.Targets[1]);
            Assert.Single(item1Config2.Labels);
            Assert.True(item1Config2.Labels.ContainsKey("host"));
            Assert.Equal("infonot-prod-facade", item1Config2.Labels["host"]);

            var item2 = config.Jobs[1];

            Assert.Equal("proxy-metrics", item2.JobName);
            Assert.Single(item2.StaticConfigs);

            var item2Config1 = item2.StaticConfigs[0];
            Assert.Single(item2Config1.Targets);
            Assert.Equal("192.168.80.203:8101", item2Config1.Targets[0]);
            Assert.Single(item2Config1.Labels);
            Assert.True(item2Config1.Labels.ContainsKey("role"));
            Assert.Equal("apigate", item2Config1.Labels["role"]);
        }
    }
}
