using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyLab.Log;

namespace MyLab.PrometheusAgent.Model
{
    public class ScrapeSourceDescription
    {
        public Uri ScrapeUrl { get; }

        public ScrapeSourceStateDescription State { get; set; }

        public IReadOnlyDictionary<string, string> Labels { get; }

        public ScrapeSourceDescription(Uri scrapeUrl, IDictionary<string, string> labels, ScrapeSourceStateDescription state)
        {
            ScrapeUrl = scrapeUrl;
            State = state;
            Labels = new ReadOnlyDictionary<string, string>(labels);
        }
    }

    public class ScrapeSourceStateDescription
    {
        public bool Enabled { get; set; }
        public string Reason { get; set; }
        public ExceptionDto Exception { get; set; }

        public ScrapeSourceStateDescription(bool enabled, string reason = null, ExceptionDto exception = null)
        {
            Enabled = enabled;
            Reason = reason;
            Exception = exception;
        }
    }
}
