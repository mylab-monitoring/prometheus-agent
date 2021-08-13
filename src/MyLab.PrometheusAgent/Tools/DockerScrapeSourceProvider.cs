using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using MyLab.Log.Dsl;
using MyLab.Log;
using MyLab.PrometheusAgent.Model;

namespace MyLab.PrometheusAgent.Tools
{
    class DockerScrapeSourceProvider : IScrapeSourceProvider, IDisposable
    {
        private readonly string _dockerSock;
        private readonly DockerDiscoveryStrategy _discoveryStrategy;
        private readonly DockerClient _client;

        public IDslLogger Log { get; set; }

        public IDictionary<string,string> AdditionalLabels { get; set; }

        public DockerScrapeSourceProvider(string dockerSock, DockerDiscoveryStrategy discoveryStrategy)
        {
            _dockerSock = dockerSock;
            _discoveryStrategy = discoveryStrategy;
            _client = new DockerClientConfiguration(
                    new Uri(dockerSock))
                .CreateClient();
        }

        public async Task<ScrapeSourceDescription[]> LoadAsync()
        {
            if(_discoveryStrategy == DockerDiscoveryStrategy.None)
                return null;

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

            return activeContainers
                .Select(ContainerToDesc)
                .ToArray();
        }

        private ScrapeSourceDescription ContainerToDesc(ContainerListResponse container)
        {
            string host = container.Names.FirstOrDefault() ?? container.NetworkSettings?.Networks?.FirstOrDefault().Value.IPAddress;

            var cLabels = new Dictionary<string,string>(container.Labels);

            if (host == null)
            {
                Log?.Warning("Can't detect container host")
                    .AndFactIs("container-id", container.ID)
                    .Write();
                return null;
            }

            int? port;

            if (cLabels.TryGetValue("metrics_port", out var portStr))
            {
                port = ushort.Parse(portStr);
                cLabels.Remove("metrics_port");
            }
            else
            {
                var foundPrivatePort = container.Ports?.FirstOrDefault()?.PrivatePort;

                if (foundPrivatePort.HasValue)
                {
                    port = foundPrivatePort;
                }
                else
                {
                    Log?.Warning("Can't detect container metric port. The 80 port will be used")
                        .AndFactIs("container-id", container.ID)
                        .Write();

                    port = 80;
                }
            }

            if (cLabels.TryGetValue("metrics_path", out var path))
            {
                cLabels.Remove("metrics_path");
            }
            else
            {
                path = "/metrics";
            }


            var normPath = path.StartsWith('/') ? path : ("/" + path);
            var normHost = host.Trim('/');

            var url = new UriBuilder
            {
                Host = normHost,
                Port = port.Value,
                Path = normPath
            }.Uri;

            var newLabels = new Dictionary<string,string>();

            foreach (var l in cLabels)
            {
                if (l.Key.StartsWith("metrics_"))
                {
                    newLabels.Add(NormKey(l.Key.Substring(8)), l.Value);
                }
                else
                {
                    newLabels.Add("container_label_" + NormKey(l.Key), l.Value);
                }
            }

            if (AdditionalLabels != null)
            {
                foreach (var l in AdditionalLabels)
                    newLabels.Add(l.Key, l.Value);
            }

            return new ScrapeSourceDescription(url, newLabels);
        }

        string NormKey(string key)
        {
            char[] buff = key.ToCharArray();

            for (int i = 0; i < buff.Length; i++)
            {
                if (!char.IsLetterOrDigit(buff[i]) && buff[i] != '_' && buff[i] != ':')
                    buff[i] = '_';
            }

            return  new string(buff);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}