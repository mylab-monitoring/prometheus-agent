using MyLab.PrometheusAgent.Tools;
using Xunit;

namespace UnitTests
{
    public class ServiceLabelExcludeLogicBehavior
    {
        [Theory]
        [InlineData("maintainer", true)]
        [InlineData("com.docker.compose.foo", false)]
        [InlineData("com.docker.compose.bar", true)]
        [InlineData("desktop.docker.baz", true)]
        [InlineData("foo", false)]
        public void ShouldDetermineExcluding(string labelName, bool expectedExcluding)
        {
            //Arrange
            var whiteList = new[] { "com.docker.compose.foo" };

            var logic = new ServiceLabelExcludeLogic(whiteList);

            //Act
            var exclude = logic.ShouldExcludeLabel(labelName);

            //Assert
            Assert.Equal(expectedExcluding, exclude);
        }
    }
}
