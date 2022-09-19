using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using System.Threading.Tasks;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class OutputFormatterBenchmarks
    {
        const string EndPoint = "/WeatherForecast?count=";

        const int IterationCount = 100;

        private readonly TestServer server;
        private readonly HttpClient jsonClient;
        private readonly HttpClient csvClient;

        public OutputFormatterBenchmarks()
        {
            // Arrange
            server = new TestServer(new WebHostBuilder()
               .UseStartup<TestApp.Startup>());
            jsonClient = server.CreateClient();
            var jsonAccept = jsonClient.DefaultRequestHeaders.Accept;
            jsonAccept.Clear();
            jsonAccept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            csvClient = server.CreateClient();
            var csvAccept = csvClient.DefaultRequestHeaders.Accept;
            csvAccept.Clear();
            csvAccept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/csv"));
            this.RecordCount = 4;
        }

        [Params(10, 100, 1000)]
        public int RecordCount { get; set; }

        [Benchmark]
        public async Task Json()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                var response = await jsonClient.GetAsync(EndPoint + RecordCount);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var l = responseString.Length;
            }
        }

        [Benchmark]
        public async Task Csv()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                var response = await csvClient.GetAsync(EndPoint + RecordCount);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var l = responseString.Length;
            }
        }
    }
}
