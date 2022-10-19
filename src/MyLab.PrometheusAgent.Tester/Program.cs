using System;
using System.IO;
using System.Threading.Tasks;
using MyLab.Log;
using MyLab.Log.Serializing;
using MyLab.PrometheusAgent;
using YamlDotNet.Serialization;

namespace MyLab.PrometheusAgent.Tester
{
    public class Program
    {
        public static async Task Main()
        {
            if (!File.Exists("metrics.txt"))
            {
                Console.WriteLine("Error: File 'metrics.txt' not found!");
            }

            try
            {
                var metricsStr = await File.ReadAllTextAsync("metrics.txt");

                using var reader = new StringReader(metricsStr);

                while (reader.Peek() != -1)
                {
                    await MetricModel.ReadAsync(reader);
                }

            }
            catch (Exception e)
            {
                var dto = ExceptionDto.Create(e);

                var serializer = new SerializerBuilder().Build();
                var yaml = serializer.Serialize(dto);

                Console.WriteLine(yaml);
            }
        }
    }
}