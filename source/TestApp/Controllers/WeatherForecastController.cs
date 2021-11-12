using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

using System.Threading.Tasks;

namespace TestApp.Controllers
{
	[ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        static List<WeatherForecast> Data;

        static WeatherForecastController()
        {
            var rng = new Random(1);
            var today = DateTime.Today;
            Data =
                Enumerable.Range(1, 1000)
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
            if (count > Data.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            return Data.Take(count);
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
        public Task<double> Baseline()
        {
            return Task.FromResult(1d);
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
}
