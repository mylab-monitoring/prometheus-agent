using Docker.DotNet;
using System;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using Xunit;

namespace IntegrationTests
{
    public class DockerNetBehavior : IAsyncLifetime
    {
        private DockerClient _client;

        public DockerNetBehavior()
        {
            
        }

        [Fact]
        public async Task ShouldGetContainerStat()
        {
            //Arrange
            var p = new ContainersListParameters
            {
                All = true
            };

            //Act
            var containers = await _client.Containers.ListContainersAsync(p);

            var target1Container = containers.FirstOrDefault(c => c.Names.Contains("/prometheus-agent-target-1"));

            //Assert
            Assert.NotNull(target1Container);
            Assert.Equal("running", target1Container.State);
        }

        public Task InitializeAsync()
        {
            _client = new DockerClientConfiguration(
                    new Uri("npipe://./pipe/docker_engine"))
                .CreateClient();

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _client.Dispose();

            return Task.CompletedTask;
        }
    }
}
