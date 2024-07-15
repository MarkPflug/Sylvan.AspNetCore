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
using System.Text;
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
	private readonly HttpClient client;

	byte[] jsonPayload;
	byte[] csvPayload;
	byte[] xlsxPayload;
	byte[] xlsbPayload;

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

		client = server.CreateClient();

		this.RecordCount = 10;

		GenerateData();
	}

	//[Params(10, 100, 1000)]
	[Params(1000)]
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

		jsonPayload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(GenerateData(RecordCount));

		{
			var ms = new MemoryStream();
			var tw = new StreamWriter(ms, Encoding.UTF8, -1, true);
			var cw = CsvDataWriter.Create(tw);
			cw.Write(GenerateData(RecordCount).AsDataReader());
			tw.Flush();
			this.csvPayload = new byte[ms.Length];
			ms.Seek(0, SeekOrigin.Begin);
			ms.Read(csvPayload, 0, csvPayload.Length);

		}

		{
			var ms = new MemoryStream();
			var s = new NoCloseStream(ms);
			using (var cw = ExcelDataWriter.Create(s, ExcelWorkbookType.ExcelXml))
			{
				cw.Write(GenerateData(RecordCount).AsDataReader());
			}
			this.xlsxPayload = new byte[ms.Length];
			ms.Seek(0, SeekOrigin.Begin);
			ms.Read(xlsxPayload, 0, xlsxPayload.Length);
		}

		{
			var ms = new MemoryStream();
			var s = new NoCloseStream(ms);
			using (var cw = ExcelDataWriter.Create(s, ExcelWorkbookType.ExcelBinary))
			{
				cw.Write(GenerateData(RecordCount).AsDataReader());
			}
			this.xlsbPayload = new byte[ms.Length];
			ms.Seek(0, SeekOrigin.Begin);
			ms.Read(xlsbPayload, 0, xlsbPayload.Length);
		}
	}

	[Benchmark]
	public async Task Baseline()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await client.GetAsync(BaselineEndPoint);
			response.EnsureSuccessStatusCode();
		}
	}

	[Benchmark]
	public async Task Json()
	{
		var content = new ByteArrayContent(jsonPayload);
		content.Headers.Add("Content-Type", "application/json");
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await client.PostAsync(EndPoint, content);
			response.EnsureSuccessStatusCode();
		}
	}

	[Benchmark]
	public async Task ExcelXlsb()
	{
		var content = new ByteArrayContent(xlsbPayload);
		content.Headers.Add("Content-Type", ExcelFileType.ExcelBinaryContentType);

		for (int i = 0; i < IterationCount; i++)
		{
			var response = await client.PostAsync(EndPoint, content);
			response.EnsureSuccessStatusCode();
		}
	}

	[Benchmark]
	public async Task ExcelXlsbData()
	{
		var content = new ByteArrayContent(xlsbPayload);
		content.Headers.Add("Content-Type", ExcelFileType.ExcelBinaryContentType);

		for (int i = 0; i < IterationCount; i++)
		{
			var response = await client.PostAsync(DataEndPoint, content);
			response.EnsureSuccessStatusCode();
		}
	}

	[Benchmark]
	public async Task ExcelXlsx()
	{
		var content = new ByteArrayContent(xlsxPayload);
		content.Headers.Add("Content-Type", ExcelFileType.ExcelXmlContentType);

		for (int i = 0; i < IterationCount; i++)
		{
			var response = await client.PostAsync(EndPoint, content);
			response.EnsureSuccessStatusCode();
		}
	}

	[Benchmark]
	public async Task ExcelXlsxData()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var content = new ByteArrayContent(xlsxPayload);
			content.Headers.Add("Content-Type", ExcelFileType.ExcelXmlContentType);

			var response = await client.PostAsync(DataEndPoint, content);
			response.EnsureSuccessStatusCode();
		}
	}

	[Benchmark]
	public async Task Csv()
	{
		var content = new ByteArrayContent(csvPayload);
		content.Headers.Add("Content-Type", "text/csv");
		//content.Headers.Add("Csv-Schema", "Date:DateTime,TemperatureC:int,TemperatureF:int,Summary");
		for (int i = 0; i < IterationCount; i++)
		{
			var response = await client.PostAsync(EndPoint, content);
			response.EnsureSuccessStatusCode();
		}
	}

	[Benchmark]
	public async Task CsvData()
	{
		for (int i = 0; i < IterationCount; i++)
		{
			var content = new ByteArrayContent(csvPayload);
			content.Headers.Add("Content-Type", "text/csv");
			//content.Headers.Add("Csv-Schema", "Date:DateTime,TemperatureC:int,TemperatureF:int,Summary");

			var response = await client.PostAsync(DataEndPoint, content);
			response.EnsureSuccessStatusCode();
		}
	}
}
