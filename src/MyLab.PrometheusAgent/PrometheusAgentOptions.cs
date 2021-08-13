using System.Collections.Generic;

namespace MyLab.PrometheusAgent
{
    public class PrometheusAgentOptions
    {
        public string Config { get; set; }
        public int? ScrapeTimeoutSec { get; set; }
        public DockerDiscoveryOpts Docker { get; set; } = new DockerDiscoveryOpts();
    }

    public class DockerDiscoveryOpts
    {
        public DockerDiscoveryStrategy Strategy { get; set; } = DockerDiscoveryStrategy.None;
        public string Socket { get; set; } = "unix:///var/run/docker.sock";
        public Dictionary<string, string> Labels { get; set; }
    }

    public enum DockerDiscoveryStrategy
    {
        None,
        All,
        Include
    }
}
