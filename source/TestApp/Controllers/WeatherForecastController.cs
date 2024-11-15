﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sylvan.Data;
using Sylvan.Data.Csv;
using Sylvan.Data.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

using System.Threading.Tasks;

namespace TestApp.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : Controller
{
	static readonly string[] Summaries = new[]
	{
		"Freezing", "Bracing", "Chilly", 
		"Cool", "Mild", "Warm", "Balmy", 
		"Hot", "Sweltering", "Scorching"
	};

	static List<WeatherForecast> Data;

	static WeatherForecastController()
	{
		var rng = new Random(1);
		var today = DateTime.Today;
		Data =
			Enumerable.Range(1, 10000)
			.Select(index => new WeatherForecast
			{
				Date = today.AddDays(index),
				TemperatureC = rng.Next(-20, 55),
				Summary = Summaries[rng.Next(Summaries.Length)]
			})
			.ToList();
	}

	[HttpGet]
	public IEnumerable<WeatherForecast> Get(int count = 4)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Data.Count);
		return Data.Take(count);
	}

	[HttpGet("GetReader")]
	public DbDataReader GetReader(int count = 4)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Data.Count);
		return Data.Take(count).AsDataReader();
	}

	[HttpGet("test")]
	public IActionResult Test()
	{
		return View();
	}

	[HttpPost("model")]
	public IActionResult Test(IEnumerable<WeatherForecast> data)
	{
		return View();
	}

	[HttpPost("model2")]
	public object Test([FromForm]IEnumerable<WeatherForecast> data, string name)
	{
		if (data == null)
			return BadRequest();
		return data.Select(f => f.TemperatureC).Max() + " " + (name ?? "unspecified");
	}

	[HttpPost("viewdata")]
	public new IActionResult ViewData([FromForm] DbDataReader data)
	{
		return View(data);
	}

	[HttpPost("data")]
	public object Test([FromForm] DbDataReader data, [FromForm] string name)
	{
		double accum = 0d;
		var idx = data.GetOrdinal("TemperatureC");
		var c = 0;
		while (data.Read())
		{
			accum += data.GetDouble(idx);
			c++;
		}
		return (accum / c) + name;
	}

	[HttpPost("postme")]
	public object PostMe(IFormFile data)
	{
		DbDataReader reader;

		var stream = data.OpenReadStream();
		switch (data.ContentType)
		{
			case "text/csv":
				var tr = new StreamReader(stream);
				reader = CsvDataReader.Create(tr);
				break;
			default:
				reader = ExcelDataReader.Create(stream, ExcelWorkbookType.ExcelXml);
				break;
		}

		var dat = reader.GetRecords<WeatherForecast>();

		return Process(dat);
	}

	double Process(IEnumerable<WeatherForecast> dat)
	{
		return dat.Select(d => d.TemperatureC).Average();
	}

	[HttpGet("View")]
	public IActionResult ViewForecast()
	{
		var data = GetReader();
		return View("ViewData", data);
	}

	async Task<SqlConnection> GetConnection()
	{
		var conn = new SqlConnection();
		conn.ConnectionString = new SqlConnectionStringBuilder
		{
			DataSource = ".",
			InitialCatalog = "weather_data",
			IntegratedSecurity = true
		}.ConnectionString;
		await conn.OpenAsync();
		return conn;
	}

	[HttpGet("GetSqlData")]
	public async Task<DbDataReader> GetData(int count)
	{
		// conn will dispose with data reader
		SqlConnection conn = await GetConnection();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = "select Date, TempCelsius, Summary from forecast";
		return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
	}

	[HttpPost("upload")]
	public async Task<double> Upload(IAsyncEnumerable<WeatherForecast> data)
	{
		double a = 0d;
		int c = 0;
		await foreach (var item in data)
		{
			var x = item;
			a += item.TemperatureC;
			c++;
		}
		return a / c;
	}

	[HttpPost("uploaddata")]
	public async Task<double> Upload(DbDataReader data)
	{
		var idx = data.GetOrdinal("TemperatureC");
		double a = 0d;
		int c = 0;
		while (await data.ReadAsync())
		{
			a += data.GetDouble(idx);
			c++;
		}
		return a / c;
	}

	[HttpGet("baseline")]
	public double Baseline()
	{
		return 1d;
	}

	
	public async Task<DbDataReader> DbTest()
	{
		// not "using", connection will be disposed when the reader is closed.
		var conn = new SqlConnection("Integrated Security=true;Data Source=.;Initial Catalog=sc_dev");
		await conn.OpenAsync();
		var cmd = conn.CreateCommand();
		cmd.CommandText = "select * from SC.Issue";
		var r = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
		return r;
	}

	//[HttpGet("db")]
	//public async Task<DbDataReader> GetDb()
	//{
	//    var c = new SQLiteConnection("Data Source=test.db");
	//    c.Open();
	//    var cmd = c.CreateCommand();
	//    cmd.CommandText = "select * from Test";
	//    var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
	//    return reader;
	//}
}
