using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyLab.Log;

namespace MyLab.PrometheusAgent
{
    public class MetricModel
    {
        public string Name { get; private set; }

        public string Type { get; private set; }

        public double Value { get; private set; }

        public long? TimeStamp { get; private set; }

        public IReadOnlyDictionary<string,string> Labels { get; private set; }

        public MetricModel(string name, string type, double value, DateTime? timeStamp, IDictionary<string,string > labels)
        {
            

            Name = name;
            Type = type;
            Value = value;
            Labels = new ReadOnlyDictionary<string, string>(labels);

            if (timeStamp.HasValue)
            {
                var timeStampFromEpoch = timeStamp.Value - new DateTime(1970, 1, 1);
                TimeStamp = (long) timeStampFromEpoch.TotalMilliseconds;
            }
        }

        MetricModel()
        {
            
        }

        public MetricModel AddLabels(IEnumerable<KeyValuePair<string, string>> addLabels)
        {
            var newLabels = new Dictionary<string, string>();

            if (Labels != null)
            {
                foreach (var label in Labels)
                {
                    newLabels.Add(label.Key, label.Value);
                }
            }

            if (addLabels != null)
            {
                foreach (var label in addLabels)
                {
                    if (newLabels.ContainsKey(label.Key))
                        newLabels[label.Key] = label.Value;
                    else
                        newLabels.Add(label.Key, label.Value);
                }
            }

            var newLabelsDict = newLabels.ToDictionary(
                itm => itm.Key,
                itm => itm.Value);

            return new MetricModel
            {
                Name = Name,
                Type = Type,
                Value = Value,
                TimeStamp = TimeStamp,
                Labels = newLabelsDict
            };
        }

        public async Task WriteAsync(StringWriter stringWriter)
        {
            if(Type != null) 
                await stringWriter.WriteLineAsync($"# TYPE {Name} {Type}");
            await stringWriter.WriteAsync($"{Name} {{");
            await stringWriter.WriteAsync(
                string.Join(",", 
                    Labels.Select(l => $"{l.Key}=\"{l.Value}\"")
                ));
            await stringWriter.WriteAsync("} ");
            await stringWriter.WriteAsync(Value.ToString("F2", CultureInfo.InvariantCulture));

            if (TimeStamp.HasValue)
                await stringWriter.WriteAsync(" " + TimeStamp.Value);

            await stringWriter.WriteLineAsync();
        }

        public static async Task<MetricModel> ReadAsync(string str)
        {
            var rdr = new StringReader(str);

            return await ReadAsync(rdr);
        }

        public static async Task<MetricModel> ReadAsync(StringReader reader)
        {
            var metric = new MetricModel();

            while (reader.Peek() == '#')
            {
                var readString = await reader.ReadLineAsync();

                if (readString != null && readString.StartsWith("# TYPE"))
                {
                    var items = readString.Substring(7).Split(' ', StringSplitOptions.TrimEntries);
                    if (items.Length != 2)
                        throw new FormatException("Type string has wrong parts")
                            .AndFactIs("typeString", readString);

                    metric.Name = items[0];
                    metric.Type = items[1];
                }
            }
            
            var bodyString = await reader.ReadLineAsync();

            if (!string.IsNullOrEmpty(bodyString))
            {
                if (metric.Name != null)
                {
                    if (!bodyString.StartsWith(metric.Name))
                        throw new FormatException("Body string has wrong name")
                            .AndFactIs("bodyString", bodyString);
                }
                else
                {
                    metric.Name = bodyString.Remove(bodyString.IndexOfAny(new[] {'{', ' '})).Trim();
                }

                var labelsStart = bodyString.IndexOf('{');
                var labelsEnd = bodyString.IndexOf('}');

                if (labelsStart != -1 && labelsEnd != -1)
                {
                    var labels = bodyString
                        .Substring(labelsStart + 1, labelsEnd - labelsStart - 1)
                        .Split(',')
                        .Select(v => new {Value = v, SplitPos = v.IndexOf('=')})
                        .ToDictionary(
                            v => v.Value.Remove(v.SplitPos).Trim(),
                            v => v.Value.Substring(v.SplitPos + 1).Trim('\"', ' '));

                    metric.Labels = new ReadOnlyDictionary<string, string>(labels);
                }

                var valueString = labelsEnd != -1
                    ? bodyString.Substring(labelsEnd + 1).Trim()
                    : bodyString.Substring(bodyString.IndexOf(' ') + 1).Trim();

                var valueParts = valueString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (valueParts.Length == 0)
                    throw new FormatException("Value parts not found in body string")
                        .AndFactIs("bodyString", bodyString);
                if (valueParts.Length > 2)
                    throw new FormatException("Too many value parts in body string")
                        .AndFactIs("bodyString", bodyString)
                        .AndFactIs("valuePartsCount", valueParts.Length);

                if (!double.TryParse(valueParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                    throw new FormatException("Value has wrong format")
                        .AndFactIs("valuePart0", valueParts[0]);

                if (valueParts.Length == 2)
                {
                    if (!long.TryParse(valueParts[1], NumberStyles.Any, CultureInfo.InvariantCulture,
                        out var timeStamp))
                        throw new FormatException("Time stamp has wrong format")
                            .AndFactIs("timeStamp", valueParts[1]);
                    
                    metric.TimeStamp = timeStamp;
                }

                metric.Value = value;
            }

            return metric;
        }
    }
}
