using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public MetricModel AddLabels(IDictionary<string, string> addLabels)
        {
            var newLabels = addLabels != null
                ? Labels
                    .Union(addLabels)
                    .ToDictionary(
                        itm => itm.Key,
                        itm => itm.Value)
                : Labels;
            return new MetricModel
            {
                Name = Name,
                Type = Type,
                Value = Value,
                TimeStamp = TimeStamp,
                Labels = newLabels
            };
        }

        public async Task Write(StringWriter stringWriter)
        {
            await stringWriter.WriteLineAsync($"# TYPE {Name} {Type}");
            await stringWriter.WriteAsync($"{Name} {{");
            await stringWriter.WriteAsync(
                string.Join(",", 
                    Labels.Select(l => $"{l.Key}={l.Value}")
                ));
            await stringWriter.WriteAsync("} ");
            await stringWriter.WriteAsync(Value.ToString("F2", CultureInfo.InvariantCulture));

            if (TimeStamp.HasValue)
                await stringWriter.WriteAsync(" " + TimeStamp.Value);

            await stringWriter.WriteLineAsync();
        }

        public static async Task<MetricModel> Read(string str)
        {
            var rdr = new StringReader(str);

            return await Read(rdr);
        }

        public static async Task<MetricModel> Read(StringReader reader)
        {
            var typeString = await reader.ReadLineAsync();

            if(string.IsNullOrEmpty(typeString))
                throw new FormatException("Type string is empty");
            if(!typeString.StartsWith("# TYPE "))
                throw new FormatException("Type string start");

            var items = typeString.Substring(7).Split(' ', StringSplitOptions.TrimEntries);
            if(items.Length != 2)
                throw new FormatException("Type string has wrong parts");

            var metric = new MetricModel
            {
                Name = items[0],
                Type = items[1]
            };

            var bodyString = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(bodyString))
                throw new FormatException("Body string is empty");

            if(!bodyString.StartsWith(metric.Name))
                throw new FormatException("Body string has wrong name");
            
            var labelsStart = bodyString.IndexOf('{');
            var labelsEnd = bodyString.IndexOf('}');

            if(labelsStart == -1 || labelsEnd == -1)
                throw new FormatException("Cant detect levels");

            var labels = bodyString
                .Substring(labelsStart + 1, labelsEnd - labelsStart-1)
                .Split(',')
                .Select(v => new { Value = v, SplitPos = v.IndexOf('=')})
                .ToDictionary(
                    v => v.Value.Remove(v.SplitPos).Trim(), 
                    v => v.Value.Substring(v.SplitPos+1).Trim('\"', ' '));

            metric.Labels = new ReadOnlyDictionary<string, string>(labels);

            var valueString = bodyString.Substring(labelsEnd + 1).Trim();

            var valueParts = valueString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if(valueParts.Length == 0)
                throw new FormatException($"Value parts not found in body string '{bodyString}'");
            if (valueParts.Length > 2)
                throw new FormatException($"Too many value parts ({valueParts.Length}) in body string '{bodyString}'");

            if (!double.TryParse(valueParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                throw new FormatException($"Value has wrong format: '{valueParts[0]}'");

            if (valueParts.Length == 2)
            {
                if (!long.TryParse(valueParts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var timeStamp))
                    throw new FormatException($"Time stemp has wrong format: '{valueParts[1]}'");
                metric.TimeStamp = timeStamp;
            }

            metric.Value = value;

            return metric;
        }
    }
}
