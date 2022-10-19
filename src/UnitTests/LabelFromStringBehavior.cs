using System.IO;
using MyLab.PrometheusAgent.Tools;
using Xunit;

namespace UnitTests
{
    public class LabelFromStringBehavior
    {
        [Theory]
        [InlineData("container_name=\"foo\"")]
        [InlineData("container_name= \"foo\" ")]
        public void ShouldReadSingleLabel(string labelsString)
        {
            //Arrange
            var stringReader = new StringReader(labelsString);

            //Act
            var label = LabelFromString.Read(stringReader);

            //Assert
            Assert.Equal("container_name", label.Name);
            Assert.Equal("foo", label.Value);
        }

        [Theory]
        [InlineData("container_name=\"foo\",mode=\"bar\"")]
        [InlineData("container_name= \"foo\" , mode = \"bar\" ")]
        public void ShouldReadNextLabel(string labelsString)
        {
            //Arrange

            var stringReader = new StringReader(labelsString);
            var label1 = LabelFromString.Read(stringReader);

            //Act

            var label2 = LabelFromString.Read(stringReader);

            //Assert
            Assert.Equal("mode", label2.Name);
            Assert.Equal("bar", label2.Value);
        }

        [Fact]
        public void ShouldReadLabelWithComma()
        {
            //Arrange
            var labelsString = "container_name=\"foo, bar\",mode=\"baz\"";

            var stringReader = new StringReader(labelsString);

            //Act

            var label = LabelFromString.Read(stringReader);

            //Assert
            Assert.Equal("container_name", label.Name);
            Assert.Equal("foo, bar", label.Value);
        }

        [Fact]
        public void ShouldReadNullIfEndOfString()
        {
            //Arrange
            var labelsString = "";

            var stringReader = new StringReader(labelsString);

            //Act
            var label = LabelFromString.Read(stringReader);

            //Assert
            Assert.Null(label);
        }
    }
}
