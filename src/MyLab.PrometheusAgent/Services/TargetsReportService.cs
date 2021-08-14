using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyLab.Log;

namespace MyLab.PrometheusAgent.Services
{
    public class TargetsReportService
    {
        private readonly object _sync = new object();

        private readonly Dictionary<string, TargetsReportItem> _items;
        public IReadOnlyDictionary<string, TargetsReportItem> Items { get; }

        public TargetsReportService()
        {
            _items = new Dictionary<string, TargetsReportItem>();
            Items = new ReadOnlyDictionary<string, TargetsReportItem>(_items);
        }

        public void Report(TargetsReportItem reportItem)
        {
            lock (_sync)
            {
                if (_items.ContainsKey(reportItem.Id))
                    _items[reportItem.Id] = reportItem;
                else
                    _items.Add(reportItem.Id, reportItem);
            }
        }
    }

    public class TargetsReportItem
    {
        public string Id { get; set; }
        public DateTime Dt { get; set; }
        public TimeSpan Duration { get; set; }
        public ExceptionDto Error { get; set; }
        public long ResponseVolume { get; set; }
        public long MetricsCount { get; set; }
    }
}
