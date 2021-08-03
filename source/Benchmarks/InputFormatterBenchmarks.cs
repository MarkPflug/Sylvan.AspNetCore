using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TestApp;

namespace Benchmarks
{
    [HtmlExporter]
    [MemoryDiagnoser]
    [SimpleJob(1, 2, 4, 1)]
    public class InputFormatterBenchmarks
    {
        const string EndPoint = "/WeatherForecast/Upload";
        const string DataEndPoint = "/WeatherForecast/Upload";
        const int IterationCount = 100;

        private readonly TestServer server;
        private readonly HttpClient jsonClient;
        private readonly HttpClient csvClient;

        byte[] jsonPayload;
        byte[] csvPayload;

        static IEnumerable<WeatherForecast> GenerateData(int count)
        {
            var date = DateTime.Today;
            var rand = new Random(1);
            for(int i = 0; i < count; i++)
            {
                yield return new WeatherForecast
                {
                    Date = date.AddDays(i),
                    Summary = "HUMID AF",
                    TemperatureC = rand.Next(2, 34),
                };
            }
        }

        public InputFormatterBenchmarks()
        {
            server = new TestServer(new WebHostBuilder()
               .UseStartup<Startup>());
            jsonClient = server.CreateClient();
            csvClient = server.CreateClient();
            this.RecordCount = 10;
        }

        [Params(10, 100, 1000)]
        public int RecordCount { get; set; }

        void GenerateData()
        {
            if (jsonPayload == null)
            {
                jsonPayload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(GenerateData(RecordCount));
            }

            if (csvPayload == null)
            {
                var ms = new MemoryStream();
                var tw = new StreamWriter(ms);
                var cw = CsvDataWriter.Create(tw);
                cw.Write(GenerateData(RecordCount).AsDataReader());
                tw.Flush();
                this.csvPayload = new byte[ms.Length];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(csvPayload, 0, csvPayload.Length);
            }
        }

        [Benchmark]
        public async Task Json()
        {
            GenerateData();
            for (int i = 0; i < IterationCount; i++)
            {
                var content = new ByteArrayContent(jsonPayload);
                content.Headers.Add("Content-Type", "application/json");

                var response = await jsonClient.PostAsync(EndPoint, content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
            }
        }

        [Benchmark]
        public async Task Csv()
        {
            GenerateData();
            for (int i = 0; i < IterationCount; i++)
            {
                var content = new ByteArrayContent(csvPayload);
                content.Headers.Add("Content-Type", "text/csv");
                content.Headers.Add("Csv-Schema", "Date:DateTime,TemperatureC:int,TemperatureF:int,Summary");

                var response = await csvClient.PostAsync(DataEndPoint, content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
            }
        }

        [Benchmark]
        public async Task CsvData()
        {
            GenerateData();
            for (int i = 0; i < IterationCount; i++)
            {
                var content = new ByteArrayContent(csvPayload);
                content.Headers.Add("Content-Type", "text/csv");
                content.Headers.Add("Csv-Schema", "Date:DateTime,TemperatureC:int,TemperatureF:int,Summary");

                var response = await csvClient.PostAsync(DataEndPoint + "data", content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
            }
        }
    }
}
