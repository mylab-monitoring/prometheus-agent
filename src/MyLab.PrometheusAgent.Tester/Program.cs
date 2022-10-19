using MyLab.Log;
using MyLab.Log.Serializing;
using MyLab.PrometheusAgent;
using YamlDotNet.Serialization;

if (!File.Exists("metrics.txt"))
{
    Console.WriteLine("Error: File 'metrics.txt' not found!");
}

try
{
    var metricsStr = await File.ReadAllTextAsync("metrics.txt");

    using var reader = new StringReader(metricsStr);

    await MetricModel.ReadAsync(reader);
}
catch (Exception e)
{
    var dto = ExceptionDto.Create(e);

    var serializer = new SerializerBuilder().Build();
    var yaml = serializer.Serialize(dto);

    Console.WriteLine(yaml);
}

