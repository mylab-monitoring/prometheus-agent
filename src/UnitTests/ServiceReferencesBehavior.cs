using System.Threading.Tasks;
using MyLab.PrometheusAgent.Tools;
using Xunit;

namespace UnitTests
{
    public class MetricTargetsReferencesBehavior
    {
        [Fact]
        public void ShouldLoadUniqueTargetsCollection()
        {
            //Arrange
            var scrapeConfig = new ScrapeConfig
            {
                Items = new []
                {
                    new ScrapeConfigItem
                    {
                        StaticConfigs = new []
                        {
                            new ScrapeStaticConfig
                            {
                                Targets = new []
                                {
                                    "foo",
                                    "bar"
                                }
                            }, 
                        }
                    },
                    new ScrapeConfigItem
                    {
                        StaticConfigs = new []
                        {
                            new ScrapeStaticConfig
                            {
                                Targets = new []
                                {
                                    "foo",
                                    "baz"
                                }
                            }
                        }
                    }
                }
            };

            //Act
            var references = MetricTargetsReferences.LoadUniqueScrapeConfig(scrapeConfig);

            //Assert
            Assert.Equal(3, references.Count);
            Assert.Contains(references, reference => reference.Id == "foo");
            Assert.Contains(references, reference => reference.Id == "bar");
            Assert.Contains(references, reference => reference.Id == "baz");
        }
    }
}
