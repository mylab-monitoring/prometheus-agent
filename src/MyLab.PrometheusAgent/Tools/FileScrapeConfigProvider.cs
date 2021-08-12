using System;
using System.IO;
using System.Threading.Tasks;
using MyLab.Log;

namespace MyLab.PrometheusAgent.Tools
{
    class FileScrapeConfigProvider : IScrapeConfigProvider
    {
        private readonly string _filePath;

        public FileScrapeConfigProvider(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<ScrapeConfig> LoadAsync()
        {
            if (!File.Exists(_filePath))
                throw new InvalidOperationException("Scrape config file not found")
                    .AndFactIs("path", _filePath);

            var cfgFileContent = await File.ReadAllTextAsync(_filePath);
            return ScrapeConfig.Parse(cfgFileContent);
        }
    }
}