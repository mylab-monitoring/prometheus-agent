using MyLab.PrometheusAgent.Tools;
using Xunit;

namespace UnitTests
{
    public class StringEscapeBehavior
    {
        [Theory]
        [InlineData("foo", "foo")]
        [InlineData("foo\"bar", "foo\\\"bar")]
        [InlineData("foo\\bar", "foo\\\\bar")]
        [InlineData("foo\\\\bar", "foo\\\\\\\\bar")]
        public void ShouldEscapeString(string test, string expected)
        {
            //Arrange

            //Act
            var actual = StringEscape.Escape(test);

            //Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("foo", "foo")]
        [InlineData("foo\"bar", "foo\"bar")]
        [InlineData("foo\\\"bar", "foo\"bar")]
        [InlineData("foo\\\\bar", "foo\\bar")]
        [InlineData("foo\\\\\\qoz", "foo\\\\qoz")]
        public void ShouldUnescapeString(string test, string expected)
        {
            //Arrange

            //Act
            var actual = StringEscape.Unescape(test);

            //Assert
            Assert.Equal(expected, actual);
        }
    }
}
