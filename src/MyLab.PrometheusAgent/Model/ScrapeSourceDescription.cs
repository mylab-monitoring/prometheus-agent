using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyLab.PrometheusAgent.Model
{
    public class ScrapeSourceDescription
    {
        public Uri ScrapeUrl { get; }

        public IReadOnlyDictionary<string, string> Labels { get; }

        public ScrapeSourceDescription(Uri scrapeUrl, IDictionary<string, string> labels)
        {
            ScrapeUrl = scrapeUrl;
            Labels = new ReadOnlyDictionary<string, string>(labels);
        }
    }
}
