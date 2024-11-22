using Sylvan.Data;
using Sylvan.Data.Excel;
using System.Net.Http.Headers;

using (ExcelDataWriter w = ExcelDataWriter.Create("dump.xlsb"))
{
	w.Write(GetData().AsDataReader());
}

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseRouting();

app.Map(
	"/Demo",
	 () =>
	 {
		 return Results.Content(TestAppMinimal.Content.Demo, "text/html");
	 }
);

app.Map(
	"/TestXlsx",
	 () =>
	 {

		 var data = GetData();
		 return data.AsExcel();
	 }
);

app.Map(
	"/TestXlsb",
	 () =>
	 {
		 var data = GetData();
		 return data.AsExcel(ExcelWorkbookType.ExcelBinary);
	 }
);

app.Map(
	"/TestCsv",
	 () =>
	 {

		 var data = GetData();
		 return Results.Extensions.Csv(data);
	 }
);

// manual content negotiation
app.Map(
	"/Test",
	IResult (HttpContext context) =>
	{
		var accept = context.Request.Headers.Accept;

		var str = accept.FirstOrDefault();

		var types = str?.Split(',');
		if (types != null)
		{

			var data = GetData();
			foreach (var typeSpec in types)
			{

				var idx = typeSpec.IndexOf(';');
				var type =
					idx >= 0
					? typeSpec.Substring(0, idx)
					: typeSpec;
				switch (type.ToLowerInvariant())
				{
					case "*/*":
					case "text/csv":
						return Results.Extensions.Csv(data);
					case ExcelFileType.ExcelBinaryContentType:
						return Results.Extensions.Excel(data, ExcelWorkbookType.ExcelBinary);
					case "application/excel": 
					case ExcelFileType.ExcelXmlContentType:
						return Results.Extensions.Excel(data, ExcelWorkbookType.ExcelXml);
					case "application/json":
						return Results.Extensions.JsonData(data);
				}
			}
		}
		// none of the requested types are supported
		return Results.StatusCode(StatusCodes.Status406NotAcceptable);
	}
);

app.Run();

static IEnumerable<TestRecord> GetData()
{
	var today = DateTime.UtcNow.Date;
	return new[]
		{
			new TestRecord(1, today, "Alpha", 123.32m),
			new TestRecord(2, today.AddDays(3), "Beta", 1023.77m),
		};
}

record TestRecord(int Id, DateTime Date, string Name, decimal Value);
