using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyLab.Log;
using MyLab.PrometheusAgent.Model;

namespace MyLab.PrometheusAgent.Tools
{
    class FileScrapeSourceProvider : IScrapeSourceProvider
    {
        private readonly string _filePath;

        public FileScrapeSourceProvider(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<ScrapeSourceDescription[]> LoadAsync()
        {
            if (!File.Exists(_filePath))
                throw new InvalidOperationException("Scrape config file not found")
                    .AndFactIs("path", _filePath);

            var cfgFileContent = await File.ReadAllTextAsync(_filePath);
            var fileCfg = ScrapeConfig.Parse(cfgFileContent);

            if (fileCfg?.Jobs == null) return null;

            var resultList = new List<ScrapeSourceDescription>(); 

            foreach (var job in fileCfg.Jobs)
            {
                if (job.StaticConfigs != null)
                {
                    foreach (var scrapeStaticConfig in job.StaticConfigs)
                    {
                        if (scrapeStaticConfig.Targets != null)
                        {
                            foreach (var target in scrapeStaticConfig.Targets)
                            {
                                var normTarget = target;

                                if (!normTarget.StartsWith("http://") && !normTarget.StartsWith("https://"))
                                    normTarget = "http://" + normTarget.TrimStart('/');

                                var url = new UriBuilder(normTarget)
                                {
                                    Path = job.MetricPath ?? "/metrics"
                                }.Uri;

                                var labels = scrapeStaticConfig.Labels != null 
                                    ? new Dictionary<string, string>(scrapeStaticConfig.Labels)
                                    : new Dictionary<string, string>();

                                if (labels.ContainsKey("instance"))
                                    labels["instance"] = url.Host + ":" + url.Port;
                                else
                                    labels.Add("instance", url.Host + ":" + url.Port);

                                if (job.JobName != null)
                                {
                                    if (labels.ContainsKey("job"))
                                        labels["job"] = job.JobName;
                                    else
                                        labels.Add("job", job.JobName);
                                }

                                var scrapeSourceState = new ScrapeSourceStateDescription(true);
                                var scrapeSourceDesc = new ScrapeSourceDescription(url, labels, scrapeSourceState);
                                
                                resultList.Add(scrapeSourceDesc);
                            }
                        }
                    }
                }
            }

            return resultList.ToArray();
        }
    }
}