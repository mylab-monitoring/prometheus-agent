using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using MyLab.Log.Dsl;
using MyLab.Log;

namespace MyLab.PrometheusAgent.Tools
{
    class DockerScrapeConfigProvider : IScrapeConfigProvider, IDisposable
    {
        private readonly string _dockerSock;
        private readonly DockerDiscoveryStrategy _discoveryStrategy;
        private readonly DockerClient _client;

        public IDslLogger Log { get; set; }

        public DockerScrapeConfigProvider(string dockerSock, DockerDiscoveryStrategy discoveryStrategy)
        {
            _dockerSock = dockerSock;
            _discoveryStrategy = discoveryStrategy;
            _client = new DockerClientConfiguration(
                    new Uri(dockerSock))
                .CreateClient();
        }

        public async Task<ScrapeConfig> LoadAsync()
        {
            if(_discoveryStrategy == DockerDiscoveryStrategy.None)
                return new ScrapeConfig();

            IList<ContainerListResponse> containerList;

            try
            {
                containerList = await _client.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true
                });
            }
            catch (HttpRequestException e)
            {
                throw new InvalidOperationException("Docker communication error", e)
                    .AndFactIs("docker-sock", _dockerSock);
            }

            var activeContainers = containerList.Where(c => c.State == "running");

            switch (_discoveryStrategy)
            {
                case DockerDiscoveryStrategy.All:

                    activeContainers = activeContainers.Where(c =>
                    {
                        if (c.Labels.TryGetValue("metrics_exclude", out var strBool))
                        {
                            if (bool.TryParse(strBool, out var excludeFlag))
                                return !excludeFlag;
                        }

                        return true;
                    });

                    break;
                case DockerDiscoveryStrategy.Include:

                    activeContainers = activeContainers.Where(c =>
                    {
                        if (c.Labels.TryGetValue("metrics_include", out var strBool))
                        {
                            if (bool.TryParse(strBool, out var includeFlag))
                                return includeFlag;
                        }

                        return false;
                    });

                    break;
            }

            return new ScrapeConfig
            {
                Jobs = new []
                {
                    new ScrapeConfigJob
                    {
                        JobName = "local-auto",
                        StaticConfigs = activeContainers
                            .Select(GetContainerInfo)
                            .ToArray()
                    } 
                }
            };
        }

        private ScrapeStaticConfig GetContainerInfo(ContainerListResponse container)
        {
            var lbls = container.Labels;

            string host = container.Names.FirstOrDefault() ?? container.NetworkSettings?.Networks?.FirstOrDefault().Value.IPAddress;

            if (host == null)
            {
                Log?.Warning("Can't detect container host")
                    .AndFactIs("container-id", container.ID)
                    .Write();
                return null;
            }

            ushort? port = lbls.TryGetValue("metrics_port", out var portStr) 
                ? ushort.Parse(portStr) 
                : container.Ports?.FirstOrDefault()?.PrivatePort;

            if (!port.HasValue)
            {
                Log?.Warning("Can't detect container metric port. The 80 port will be used")
                    .AndFactIs("container-id", container.ID)
                    .Write();

                port = 80;
            }

            if (!lbls.TryGetValue("metrics_path", out var path))
                path = "/metrics";


            var normPath = path.StartsWith('/') ? path : ("/" + path);
            var normHost = host.Trim('/');

            string url = $"http://{normHost}:{port}{normPath}";

            return new ScrapeStaticConfig
            {
                Labels = lbls.ToDictionary(l => l.Key, l => "container_label_" + l.Value),
                Targets = new []{ url }
            };
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}