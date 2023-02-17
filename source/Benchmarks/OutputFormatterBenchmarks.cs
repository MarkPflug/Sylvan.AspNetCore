using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Benchmarks;

[MemoryDiagnoser]
public class OutputFormatterBenchmarks
{
	const string EndPoint = "/WeatherForecast?count=";
	const string DataEndPoint = "/WeatherForecast/GetReader?count=";

	const int IterationCount = 100;

	private readonly TestServer server;
	private readonly HttpClient jsonClient;
	private readonly HttpClient csvClient;
	private readonly HttpClient csvDataClient;
	private readonly HttpClient excelClient;

	public OutputFormatterBenchmarks()
	{
		// Arrange
		server = new TestServer(new WebHostBuilder()
		   .UseStartup<TestApp.Startup>());

		jsonClient = server.CreateClient();
		var jsonAccept = jsonClient.DefaultRequestHeaders.Accept;
		jsonAccept.Clear();
		jsonAccept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		csvClient = server.CreateClient();
		var csvAccept = csvClient.DefaultRequestHeaders.Accept;
		csvAccept.Clear();
		csvAccept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));

		csvDataClient = server.CreateClient();
		var csvDataAccept = csvDataClient.DefaultRequestHeaders.Accept;
		csvDataAccept.Clear();
		csvDataAccept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));

		excelClient = server.CreateClient();
		var ecxelAccept = excelClient.DefaultRequestHeaders.Accept;
		ecxelAccept.Clear();
		ecxelAccept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));


		this.RecordCount = 4;
	}

	[Params(10, 100, 1000)]
	public int RecordCount { get; set; }

	//[Benchmark]
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

	//[Benchmark]
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

	//[Benchmark]
	public async Task CsvData()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await csvDataClient.GetAsync(DataEndPoint + RecordCount);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			var l = responseString.Length;
		}
	}

	[Benchmark]
	public async Task Excel()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await excelClient.GetAsync(EndPoint + RecordCount);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			var l = responseString.Length;
		}
	}
}
