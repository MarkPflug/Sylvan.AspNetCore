using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
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

        [HttpGet]
        public IEnumerable<WeatherForecast> Get(int count = 4)
        {
            var rng = new Random(1);
            var today = DateTime.Today;
            var data =
                Enumerable.Range(1, count)
                .Select(index => new WeatherForecast
                {
                    Date = today.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();

            return data;
        }

        [HttpPost("upload")]
        public async Task<double> Upload(IAsyncEnumerable<WeatherForecast> data)
        {
            var r = await data.AverageAsync(a => a.TemperatureC);
            return r;
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

        [HttpPost("input")]
        public async Task<DbDataReader> GetDb(DbDataReader data)
        {
            return data;
            //while (await data.ReadAsync())
            //{
            //    var a = data.GetString(0);
            //}
            //return null;
        }

        [HttpGet("db")]
        public async Task<DbDataReader> GetDb()
        {
            var c = new SQLiteConnection("Data Source=test.db");
            c.Open();
            var cmd = c.CreateCommand();
            cmd.CommandText = "select * from Test";
            var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            return reader;
        }
    }
}
