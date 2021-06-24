using System.IO;
using System.Text;
using System.Threading.Tasks;
using MyLab.PrometheusAgent;
using Xunit;

namespace UnitTests
{
    public class MetricModelBehavior
    {
        [Theory]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric{label1=\"value1\",label2=\"value2\"} 1.1")]
        [InlineData("# TYPE foo_metric gauge\nfoo_metric { label1 = \"value1\" , label2 = \"value2\" } 1.1")]
        public async Task ShouldRead(string metricString)
        {
            //Arrange
            var reader =  new StringReader(metricString);

            //Act
            var metric = await MetricModel.Read(reader);


            //Assert
            Assert.Equal("foo_metric", metric.Name);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal(2, metric.Labels.Count);
            Assert.Equal("value1", metric.Labels["label1"]);
            Assert.Equal("value2", metric.Labels["label2"]);
            Assert.Equal(1.1d, metric.Value);
        }
    }
}
