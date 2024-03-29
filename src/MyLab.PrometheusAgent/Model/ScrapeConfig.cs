﻿using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace MyLab.PrometheusAgent.Model
{
    public class ScrapeConfig
    {
        static readonly IDeserializer Deserializer =
            new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

        [YamlMember(Alias = "scrape_configs")]
        public ScrapeConfigJob[] Jobs { get; set; }
        
        public static ScrapeConfig Parse(string yaml)
        {
            return Deserializer.Deserialize<ScrapeConfig>(yaml);
        }
    }

    public class ScrapeConfigJob
    {
        [YamlMember(Alias = "job_name")]
        public string JobName { get; set; }

        [YamlMember(Alias = "metrics_path")]
        public string MetricPath { get; set; }

        [YamlMember(Alias = "static_configs")]
        public ScrapeStaticConfig[] StaticConfigs { get; set; }
    }

    public class ScrapeStaticConfig
    {
        [YamlMember(Alias = "targets")]
        public string[] Targets { get; set; }

        [YamlMember(Alias = "labels")]
        public Dictionary<string, string> Labels { get; set; }
    }
}
