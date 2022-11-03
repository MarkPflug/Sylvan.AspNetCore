using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Sylvan.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Csv;
using Sylvan.Data.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TestApp;

namespace Benchmarks;

[MemoryDiagnoser]
public class InputFormatterBenchmarks
{
	const string BaselineEndPoint = "/WeatherForecast/Baseline";
	const string EndPoint = "/WeatherForecast/Upload";
	const string DataEndPoint = "/WeatherForecast/uploaddata";
	const int IterationCount = 100;

	private readonly TestServer server;
	private readonly HttpClient jsonClient;
	private readonly HttpClient csvClient;
	private readonly HttpClient excelClient;

	byte[] jsonPayload;
	byte[] csvPayload;
	byte[] excelPayload;

	static IEnumerable<WeatherForecast> GenerateData(int count)
	{
		var date = DateTime.Today;
		var rand = new Random(1);
		for (int i = 0; i < count; i++)
		{
			yield return new WeatherForecast
			{
				Date = date.AddDays(i),
				Summary = "Hot and humid.",
				TemperatureC = rand.Next(2, 34),
			};
		}
	}

	public InputFormatterBenchmarks()
	{
		server = new TestServer(new WebHostBuilder().UseStartup<Startup>());

		jsonClient = server.CreateClient();
		csvClient = server.CreateClient();
		excelClient = server.CreateClient();
		this.RecordCount = 10;
	}

	[Params(10, 100, 1000)]
	public int RecordCount { get; set; }

	double average;
	string averageStr;

	void GenerateData()
	{
		if (averageStr == null)
		{
			this.average = GenerateData(RecordCount).Average(r => r.TemperatureC);
			this.averageStr = average.ToString();
		}

		if (jsonPayload == null)
		{
			jsonPayload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(GenerateData(RecordCount));
		}

		if (csvPayload == null)
		{
			var ms = new MemoryStream();
			var s = new NoCloseStream(ms);
			var tw = new StreamWriter(s);
			var cw = CsvDataWriter.Create(tw);
			cw.Write(GenerateData(RecordCount).AsDataReader());
			tw.Flush();
			this.csvPayload = new byte[ms.Length];
			ms.Seek(0, SeekOrigin.Begin);
			ms.Read(csvPayload, 0, csvPayload.Length);
		}

		if (excelPayload == null)
		{
			var ms = new MemoryStream();
			var s = new NoCloseStream(ms);
			using (var cw = ExcelDataWriter.Create(s, ExcelWorkbookType.ExcelXml))
			{
				cw.Write(GenerateData(RecordCount).AsDataReader());
			}
			this.excelPayload = new byte[ms.Length];
			ms.Seek(0, SeekOrigin.Begin);
			ms.Read(excelPayload, 0, excelPayload.Length);
		}
	}

	[Benchmark]
	public async Task Baseline()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await jsonClient.GetAsync(BaselineEndPoint);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
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
	public async Task Excel()
	{
		GenerateData();
		for (int i = 0; i < IterationCount; i++)
		{
			var content = new ByteArrayContent(excelPayload);
			content.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

			var response = await excelClient.PostAsync(EndPoint, content);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			if (responseString != this.averageStr)
				throw new Exception();
		}
	}

	[Benchmark]
	public async Task ExcelData()
	{
		GenerateData();
		for (int i = 0; i < IterationCount; i++)
		{
			var content = new ByteArrayContent(excelPayload);
			content.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

			var response = await excelClient.PostAsync(DataEndPoint, content);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			if (responseString != this.averageStr)
				throw new Exception();
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
			//content.Headers.Add("Csv-Schema", "Date:DateTime,TemperatureC:int,TemperatureF:int,Summary");

			var response = await csvClient.PostAsync(EndPoint, content);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			if (responseString != this.averageStr)
				throw new Exception();
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
			//content.Headers.Add("Csv-Schema", "Date:DateTime,TemperatureC:int,TemperatureF:int,Summary");

			var response = await csvClient.PostAsync(DataEndPoint, content);
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
		}
	}
}
