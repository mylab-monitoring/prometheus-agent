using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MyLab.Log;
using MyLab.Log.Dsl;
using MyLab.PrometheusAgent.Services;

namespace MyLab.PrometheusAgent.Model
{
    class MetricSource : IDisposable
    {
        public string Id { get; }
        public HttpClient HttpClient { get; }
        public IReadOnlyDictionary<string, string> Labels { get;  }

        public IDslLogger Log { get; set; }

        public MetricSource(Uri endpoint, TimeSpan scrapeTimeout, IEnumerable<KeyValuePair<string,string>> labels)
        {
            Id = endpoint.OriginalString;
            Labels = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(labels));

            try
            {
                HttpClient = new HttpClient
                {
                    BaseAddress = endpoint,
                    Timeout = scrapeTimeout
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<MetricsScrapingResult> ScrapeMetricsAsync()
        {
            TargetsReportItem report = new TargetsReportItem
            {
                Dt = DateTime.Now,
                Id = Id
            };

            HttpResponseMessage httpResponse;

            var scrapingResult = new MetricsScrapingResult
            {
                Report = report
            };

            try
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    httpResponse = await HttpClient.GetAsync("");
                    report.ResponseVolume = httpResponse.Content.Headers.ContentLength.GetValueOrDefault(-1);
                }
                finally
                {
                    sw.Stop();
                    report.Duration = sw.Elapsed;
                }


                if (httpResponse.IsSuccessStatusCode)
                {
                    var mediaType = httpResponse.Content.Headers.ContentType?.MediaType;
                    if (mediaType != null && mediaType != "text/plain")
                    {
                        throw new InvalidOperationException("Metrics response content-type specified and not supported")
                            .AndFactIs("content-type", mediaType);
                    }

                    try
                    {
                        var stringContent = await httpResponse.Content.ReadAsStringAsync();
                        var reader = new StringReader(stringContent);

                        var metrics = new List<MetricModel>();

                        while (reader.Peek() != -1)
                        {
                            var loadedMetric = await MetricModel.ReadAsync(reader);

                            if(Labels != null)
                                loadedMetric = loadedMetric.AddLabels(Labels);

                            metrics.Add(loadedMetric);
                        }

                        scrapingResult.Metrics = metrics.ToArray();

                        report.MetricsCount = scrapingResult.Metrics.Length;
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Metrics parsing error", e);
                    }

                }
                else
                {
                    throw new InvalidOperationException("Metric target return bad response code")
                        .AndFactIs("http-code", $"{(int) httpResponse.StatusCode}({httpResponse.ReasonPhrase})");
                }
            }
            catch (Exception e)
            {
                report.Error = ExceptionDto.Create(e);

                Log?.Error(e)
                    .AndFactIs("target", Id)
                    .Write();
            }

            return scrapingResult;
        }

        public void Dispose()
        {
            HttpClient?.Dispose();
        }
    }
}
