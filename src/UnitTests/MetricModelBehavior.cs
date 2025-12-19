using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MyLab.PrometheusAgent;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class MetricModelBehavior
    {
        private readonly ITestOutputHelper _output;

        public MetricModelBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("foo_metric{label1=\"value1\",label2=\"value2\"} 1.1", null, false)]
        [InlineData("foo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1", null, false)]
        [InlineData("foo_metric{label1=\"value1\",label2=\"value2\"} 1.1 1624868358000", "28.06.2021 08:19:18", false)]
        [InlineData("foo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1 1624868358000", "28.06.2021 08:19:18", false)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1", null, true)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1", null, true)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1 1624868358000", "28.06.2021 08:19:18", true)]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1 1624868358000", "28.06.2021 08:19:18", true)]
        public async Task ShouldRead(string metricString, string dateTime, bool hasType)
        {
            //Arrange
            var reader =  new StringReader(metricString);
            DateTime? expectedDateTime = null;

            if(dateTime != null)
                expectedDateTime = DateTime.ParseExact(dateTime, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            //Act
            var metric = await MetricModel.ReadAsync(reader);


            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal(2, metric.Labels.Count);
            Assert.Equal("value1", metric.Labels["label1"]);
            Assert.Equal("value2", metric.Labels["label2"]);
            Assert.Equal(1.1d, metric.Value);

            if (expectedDateTime.HasValue)
            {
                var timeStampFromEpoch = expectedDateTime.Value - new DateTime(1970, 1, 1);
                Assert.Equal(timeStampFromEpoch.TotalMilliseconds, metric.TimeStamp.Value);
            }

            if(hasType)
                Assert.Equal("gauge", metric.Type);
            else
            {
                Assert.Null(metric.Type);
            }
        }
        
        [Fact]
        public async Task ShouldReadWithDifficultLabels()
        {
            //Arrange
            var metricString =
                "container_cpu_jiffies_total{container_name=\"portainer\",mode=\"user\",container_label_com_docker_desktop_extension_api_version=\">= 0.2.2\"," +
                "container_label_com_docker_desktop_extension_icon=\"https://portainer-io-assets.sfo2.cdn.digitaloceanspaces.com/logos/portainer.png\",container_label_com_" +
                "docker_extension_additional_urls=\"[{\\\"title\\\":\\\"Website\\\",\\\"url\\\":\\\"https://www.portainer.io?utm_campaign=DockerCon&utm_source=DockerDesktop" +
                "\\\"},{\\\"title\\\":\\\"Documentation\\\",\\\"url\\\":\\\"https://docs.portainer.io\\\"},{\\\"title\\\":\\\"Support\\\",\\\"url\\\":\\\"https://join.slack.com" +
                "/t/portainer/shared_invite/zt-txh3ljab-52QHTyjCqbe5RibC2lcjKA\\\"}]\",container_label_com_docker_extension_detailed_description=\"<p data-renderer-start-pos=\\\"" +
                "226\\\">Portainer&rsquo;s Docker Desktop extension gives you access to all of Portainer&rsquo;s rich management functionality within your docker desktop experience." +
                "</p><h2 data-renderer-start-pos=\\\"374\\\">With Portainer you can:</h2><ul><li>See all your running containers</li><li>Easily view all of your container logs</li>" +
                "<li>Console into containers</li><li>Easily deploy your code into containers using a simple form</li><li>Turn your YAML into custom templates for easy reuse</li></ul>" +
                "<h2 data-renderer-start-pos=\\\"660\\\">About Portainer&nbsp;</h2><p data-renderer-start-pos=\\\"680\\\">Portainer is the worlds&rsquo; most popular universal container" +
                " management platform with more than 650,000 active monthly users. Portainer can be used to manage Docker Standalone, Kubernetes, Docker Swarm and Nomad environments through" +
                " a single common interface. It includes a simple GitOps automation engine and a Kube API.&nbsp;</p><p data-renderer-start-pos=\\\"1006\\\">Portainer Business Edition " +
                "is our fully supported commercial grade product for business-wide use. It includes all the functionality that businesses need to manage containers at scale. Visit " +
                "<a class=\\\"sc-jKJlTe dPfAtb\\\" href=\\\"http://portainer.io/\\\" title=\\\"http://Portainer.io\\\" data-renderer-mark=\\\"true\\\">Portainer.io</a> to learn " +
                "more about Portainer Business and <a class=\\\"sc-jKJlTe dPfAtb\\\" href=\\\"http://portainer.io/take-3?utm_campaign=DockerCon&amp;utm_source=Docker%20Desktop\\\" " +
                "title=\\\"http://portainer.io/take-3?utm_campaign=DockerCon&amp;utm_source=Docker%20Desktop\\\" data-renderer-mark=\\\"true\\\">get 3 free nodes.</a></p>\"," +
                "container_label_com_docker_extension_publisher_url=\"https://www.portainer.io\",container_label_com_docker_extension_screenshots=\"[{\\\"alt\\\": \\\"screenshot one\\\"" +
                ", \\\"url\\\": \\\"https://portainer-io-assets.sfo2.digitaloceanspaces.com/screenshots/docker-extension-1.png\\\"},{\\\"alt\\\": \\\"screenshot two\\\", \\\"url\\\": " +
                "\\\"https://portainer-io-assets.sfo2.digitaloceanspaces.com/screenshots/docker-extension-2.png\\\"},{\\\"alt\\\": \\\"screenshot three\\\", \\\"url\\\": \\\"https:" +
                "//portainer-io-assets.sfo2.digitaloceanspaces.com/screenshots/docker-extension-3.png\\\"},{\\\"alt\\\": \\\"screenshot four\\\", \\\"url\\\": \\\"https://portainer-" +
                "io-assets.sfo2.digitaloceanspaces.com/screenshots/docker-extension-4.png\\\"},{\\\"alt\\\": \\\"screenshot five\\\", \\\"url\\\": \\\"https://portainer-io-assets.sfo2" +
                ".digitaloceanspaces.com/screenshots/docker-extension-5.png\\\"},{\\\"alt\\\": \\\"screenshot six\\\", \\\"url\\\": \\\"https://portainer-io-assets.sfo2.digitaloceanspaces" +
                ".com/screenshots/docker-extension-6.png\\\"},{\\\"alt\\\": \\\"screenshot seven\\\", \\\"url\\\": \\\"https://portainer-io-assets.sfo2.digitaloceanspaces.com/screenshots" +
                "/docker-extension-7.png\\\"},{\\\"alt\\\": \\\"screenshot eight\\\", \\\"url\\\": \\\"https://portainer-io-assets.sfo2.digitaloceanspaces.com/screenshots/docker-extension-8." +
                "png\\\"},{\\\"alt\\\": \\\"screenshot nine\\\", \\\"url\\\": \\\"https://portainer-io-assets.sfo2.digitaloceanspaces.com/screenshots/docker-extension-9.png\\\"}]\"," +
                "container_label_io_portainer_server=\"true\"} 332874";
            
            var reader =  new StringReader(metricString);

            //Act
            var metric = await MetricModel.ReadAsync(reader);

            //Assert
            Assert.Equal("container_cpu_jiffies_total", metric.Name);
            Assert.Equal(9, metric.Labels.Count);
            Assert.Contains(metric.Labels, l => l.Key == "container_name");
            Assert.Contains(metric.Labels, l => l.Key == "mode");
            Assert.Contains(metric.Labels, l => l.Key == "container_label_com_docker_desktop_extension_api_version");
            Assert.Contains(metric.Labels, l => l.Key == "container_label_com_docker_desktop_extension_icon");
            Assert.Contains(metric.Labels, l => l.Key == "container_label_com_docker_extension_additional_urls");
            Assert.Contains(metric.Labels, l => l.Key == "container_label_com_docker_extension_detailed_description");
            Assert.Contains(metric.Labels, l => l.Key == "container_label_com_docker_extension_publisher_url");
            Assert.Contains(metric.Labels, l => l.Key == "container_label_com_docker_extension_screenshots");
            Assert.Contains(metric.Labels, l => l.Key == "container_label_io_portainer_server");
            Assert.Equal(332874d, metric.Value);
            Assert.Null(metric.Type);
        }

        [Fact]
        public async Task ShouldReadEscaped()
        {
            //Arrange
            var reader = new StringReader("foo_metric{label1=\"value1\",label2=\"value\\\"2\"} 1.1");
            
            //Act
            var metric = await MetricModel.ReadAsync(reader);


            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal(2, metric.Labels.Count);
            Assert.Equal("value1", metric.Labels["label1"]);
            Assert.Equal("value\"2", metric.Labels["label2"]);
            Assert.Equal(1.1d, metric.Value);
        }

        [Fact]
        public async Task ShouldReadMetricsWithoutBody()
        {
            //Arrange
            var reader=  new StringReader("# TYPE foo_metric gauge");

            //Act
            var metric = await MetricModel.ReadAsync(reader);

            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal("gauge", metric.Type);
            Assert.Null(metric.TimeStamp);
            Assert.Null(metric.Labels);
            Assert.Equal(0, metric.Value);
        }

        [Fact]
        public async Task ShouldReadSimpleMetrics()
        {
            //Arrange
            var reader = new StringReader("dotnet_total_memory_bytes 6308464");
            
            //Act
            var metric = await MetricModel.ReadAsync(reader);
            
            //Assert
            Assert.Equal("dotnet_total_memory_bytes", metric.Name);
            Assert.Null(metric.Labels);
            Assert.Equal(6308464d, metric.Value);
            Assert.Null(metric.Type);
            Assert.Null(metric.TimeStamp);
        }

        [Fact]
        public async Task ShouldParseRealMetrics()
        {
            //Arrange
            var realMetricsString = await File.ReadAllTextAsync("docker-peeker-metrics.txt");
            var rdr = new StringReader(realMetricsString);

            //Act
            while (rdr.Peek() != -1)
            {
                await MetricModel.ReadAsync(rdr);
            }

            //Assert

        }

        [Fact]
        public async Task ShouldParseCrazyMetrics()
        {
            //Arrange
            var realMetricsString = await File.ReadAllTextAsync("crazy-metrics.txt");
            var rdr = new StringReader(realMetricsString);

            //Act
            while (rdr.Peek() != -1)
            {
                await MetricModel.ReadAsync(rdr);
            }

            //Assert

        }
    }
}
