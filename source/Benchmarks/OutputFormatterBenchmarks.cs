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
	private readonly HttpClient xlsxClient;
	private readonly HttpClient xlsbClient;
	private readonly HttpClient xlsClient;

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

		xlsxClient = server.CreateClient();
		var xlsxAccept = xlsxClient.DefaultRequestHeaders.Accept;
		xlsxAccept.Clear();
		xlsxAccept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

		xlsbClient = server.CreateClient();
		var xlsbAccept = xlsbClient.DefaultRequestHeaders.Accept;
		xlsbAccept.Clear();
		xlsbAccept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.ms-excel.sheet.binary.macroEnabled.12"));

		xlsClient = server.CreateClient();
		var xlsAccept = xlsClient.DefaultRequestHeaders.Accept;
		xlsAccept.Clear();
		xlsAccept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.ms-excel"));


		//this.RecordCount = 4;
	}

	//[Params(10, 100, 1000)]
	[Params(5000)]
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

	[Benchmark]
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
	public async Task ExcelXlsx()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await xlsxClient.GetAsync(EndPoint + RecordCount);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			var l = responseString.Length;
		}
	}

	[Benchmark]
	public async Task ExcelXlsb()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await xlsbClient.GetAsync(EndPoint + RecordCount);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			var l = responseString.Length;
		}
	}

	[Benchmark]
	public async Task ExcelXls()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await xlsClient.GetAsync(EndPoint + RecordCount);
			response.EnsureSuccessStatusCode();
		}
	}
}
