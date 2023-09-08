using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Sylvan.AspNetCore.Mvc.Formatters;
using Sylvan.AspNetCore.Mvc.JsonData;
using Sylvan.Data.Csv;
using System.Text;

namespace TestApp;

public class Startup
{
	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public IConfiguration Configuration { get; }

	public void ConfigureServices(IServiceCollection services)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		services.AddControllers(
			o =>
			{
				o.OutputFormatters.Insert(0, new JsonDataOutputFormatter());
				o.AddSylvanCsvFormatters();
				o.AddSylvanExcelFormatters();
			}
		);

		services.AddOptions<CsvDataReaderOptions>().BindConfiguration("Csv");
		services.AddMemoryCache();
		services.AddRazorPages();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestApp", Version = "v1" });
		});
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
			app.UseSwagger();
			app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApp v1"));
		}

		app.UseHttpsRedirection();

		app.UseRouting();
		app.UseAuthorization();

		
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
		});
	}
}
