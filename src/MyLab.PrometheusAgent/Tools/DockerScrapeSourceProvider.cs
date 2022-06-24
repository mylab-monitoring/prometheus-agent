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
        public ServiceLabelExcludeLogic ExcludeLogic { get; set; }

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

            var resultSources = new List<ScrapeSourceDescription>();

            foreach (var container in containerList)
            {
                ScrapeSourceStateDescription stateDesc;

                if (container.State == "running")
                {
                    switch (_discoveryStrategy)
                    {
                        case DockerDiscoveryStrategy.All:

                            bool exclude =
                                container.Labels.TryGetValue("metrics_exclude", out var exclBool) &&
                                bool.TryParse(exclBool, out var excludeFlag) &&
                                excludeFlag;

                            stateDesc = exclude
                                ? new ScrapeSourceStateDescription(false, "metrics_exclude")
                                : new ScrapeSourceStateDescription(true, "DockerDiscoveryStrategy.All");

                            break;
                        case DockerDiscoveryStrategy.Include:

                            bool include =
                                container.Labels.TryGetValue("metrics_include", out var inclBool) &&
                                bool.TryParse(inclBool, out var includeFlag) &&
                                includeFlag;

                            stateDesc = include
                                ? new ScrapeSourceStateDescription(true, "metrics_include")
                                : new ScrapeSourceStateDescription(false, "DockerDiscoveryStrategy.Include");

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    stateDesc = new ScrapeSourceStateDescription(false, $"Container.State != 'running' ({container.State})");
                }

                resultSources.Add(ContainerToDesc(container, stateDesc));
            }

            return resultSources.ToArray();
        }

        private ScrapeSourceDescription ContainerToDesc(ContainerListResponse container, ScrapeSourceStateDescription stateDescription)
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

            var port = RetrievePort(container, cLabels);
            var path = RetrieveMetricPath(cLabels);
            
            var normHost = host.Trim('/');

            var url = new UriBuilder
            {
                Host = normHost,
                Port = port,
                Path = path
            }.Uri;

            var newLabels = RetrieveLabels(cLabels);

            newLabels.Add("instance", $"{normHost}:{port}");
            newLabels.Add("container_name", normHost);
            
            return new ScrapeSourceDescription(url, newLabels, stateDescription);
        }

        private Dictionary<string, string> RetrieveLabels(Dictionary<string, string> cLabels)
        {
            var newLabels = new Dictionary<string, string>();

            foreach (var l in cLabels)
            {
                if (ExcludeLogic != null && ExcludeLogic.ShouldExcludeLabel(l.Key))
                    continue;

                if (l.Key.StartsWith("metrics_"))
                {
                    newLabels.Add(NormKey(l.Key.Substring(8)), l.Value);
                }
                else
                {
                    newLabels.Add("container_label_" + NormKey(l.Key), l.Value.Replace('\"', '\''));
                }
            }

            if (AdditionalLabels != null)
            {
                foreach (var l in AdditionalLabels)
                    newLabels.Add(l.Key, l.Value);
            }

            return newLabels;
        }

        private static string RetrieveMetricPath(Dictionary<string, string> cLabels)
        {
            if (cLabels.TryGetValue("metrics_path", out var path))
            {
                cLabels.Remove("metrics_path");
            }
            else
            {
                path = "/metrics";
            }

            return path.StartsWith('/') ? path : ("/" + path);
        }

        private int RetrievePort(ContainerListResponse container, Dictionary<string, string> cLabels)
        {
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

            return port.Value;
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