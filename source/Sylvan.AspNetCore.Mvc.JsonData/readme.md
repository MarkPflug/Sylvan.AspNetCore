# Sylvan.AspNetCore.Mvc.JsonData

This package provides a JSON formatter for DbDataReader responses.
The standard MVC JSON formatter expects a strongly-typed object or collection.
This formatter allows returning a `DbDataReader`, which might represent a dynamic result set
for which there is no corresponding .NET class representation. 
Internally, it uses System.Text.Json and thus provides performance comparable to the 
standard JSON formatter.

This formatter must be registered *before* the standard JSON formatter, or the standard formatter
will preempt it.

## Registration

```CSharp
public class Startup 
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddControllers(
			o =>
			{
				o.OutputFormatters.Insert(0, new JsonDataOutputFormatter());
			}
		);
		// ...
	}
}
```

## Usage

You can then write a controller action that returns a `DbDataReader` directly:

```CSharp
public class MyDataController : Controller
{

	public DbConnection GetConnection()
	{
		// ...
	}

	public async Task<DbDataReader> GetData(string query)
	{
		// not "using", connection will be disposed when the reader is closed.
		var conn = GetConnection();
		await conn.OpenAsync();
		var cmd = conn.CreateCommand();
		ValidateQuery(query); // ensure
		cmd.CommandText = query;
		return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
	}
```

Without the Sylvan.AspNetCore.Mvc.JsonData formatter registered, the standard formatter would produce a JSON payload with an array of empty objects, since DbDataReader implements `IEnumerable`.