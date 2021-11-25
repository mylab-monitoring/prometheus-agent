using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;

namespace MyLab.PrometheusAgent.Model
{
    public class MetricReport : Collection<MetricReportItem>
    {
        public MetricReport(IList<MetricReportItem> initial) : base(initial)
        {
            
        }

        public static MetricReport Create(MetricModel[] metrics, ILogger<MetricReport> logger = null)
        {
            var reportData = new Dictionary<string, (string Name, string Type, List<MetricModel> Metrics)>();

            foreach (var metric in metrics)
            {
                if (metric.Name != null)
                {
                    if (reportData.TryGetValue(metric.Name, out var foundItem))
                    {
                        foundItem.Type ??= metric.Type;

                        if (metric.Type != null && foundItem.Type != metric.Type)
                        {
                            logger?.Dsl().Error("Metric type conflict. Second metric will be ignored.")
                                .AndFactIs("metric-name", metric.Name)
                                .AndFactIs("metric-type1", foundItem.Type)
                                .AndFactIs("metric-type2", metric.Type)
                                .Write();
                        }
                        else
                        {
                            foundItem.Metrics.Add(metric);
                        }
                    }
                    else
                    {
                        reportData.Add(metric.Name, (metric.Name, metric.Type, new List<MetricModel> {metric}));
                    }
                }
            }

            return new MetricReport( 
                    reportData.Values.Select(itm => new MetricReportItem
                    {
                        Type = itm.Type,
                        Name = itm.Name,
                        Values = itm.Metrics.Select(m => new MetricReportItemValue
                        {
                            TimeStamp = m.TimeStamp,
                            Value = m.Value,
                            Labels = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(m.Labels))
                        }).ToArray()
                    }).ToList()
                );
        }

        public async Task WriteAsync(StringWriter writer)
        {
            foreach (var item in Items)
            {
                if (item.Type != null)
                    await writer.WriteLineAsync($"# TYPE {item.Name} {item.Type}");
                if (item.Values != null && item.Values.Length != 0)
                {
                    foreach (var itemValue in item.Values)
                    {
                        await writer.WriteAsync($"{item.Name} {{");
                        await writer.WriteAsync(
                            string.Join(",",
                                itemValue.Labels.Select(l => $"{l.Key}=\"{l.Value}\"")
                            ));
                        await writer.WriteAsync("} ");
                        await writer.WriteAsync(itemValue.Value.ToString("F2", CultureInfo.InvariantCulture));

                        if (itemValue.TimeStamp.HasValue)
                            await writer.WriteAsync(" " + itemValue.TimeStamp.Value);

                        await writer.WriteLineAsync();
                    }
                }
            }
        }
    }
}
