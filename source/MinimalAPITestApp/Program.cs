using Sylvan.Data;
using Sylvan.Data.Excel;

using (ExcelDataWriter w = ExcelDataWriter.Create("dump.xlsb"))
{
	w.Write(GetData().AsDataReader());
}

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseRouting();

app.Map(
	"/TestXlsx", 
	 () => {

		 var data = GetData();
		return Results.Extensions.Excel(data);
	}
);

app.Map(
	"/TestXlsb",
	 () => {

		 var data = GetData();
		 return Results.Extensions.Excel(data, ExcelWorkbookType.ExcelBinary);
	 }
);

app.Map(
	"/TestCsv",
	 () => {

		 var data = GetData();
		 return Results.Extensions.Csv(data);
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
