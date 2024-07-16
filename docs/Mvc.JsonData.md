# Sylvan.AspNetCore.Mvc.JsonData

This package provides support for returning JSON responses where the result object is a `DbDataReader` (or `IDataReader`).
The normal JSON formatter can handle serializing .NET objects/collections, but requires that the return type is statically known at compile time.
Support for DbDataReader allows returning arbitrary/dynamic data directly from SQL queries or other data sets where the shape of
the result set is only known at runtime.

## Registering the JSON data formatter

The `JsonDataOutputFormatter` can be registered in the startup `ConfigureServices`. 
It must be inserted at the front of the `OutputFormatters` collection to give it 
priority over the standard ASP.NET JSON formatter.

```C#
services.AddControllers(
	o =>
	{
		o.OutputFormatters.Insert(0, new JsonDataOutputFormatter());			
	}
);
```

## Usage in MVC

This trivial example shows how this formatter can be used in an MVC action to return a SqlServer result set
via `SqlDataReader`:

```C#

[HttpGet]
public async Task<DbDataReader> GetData()
{
	// conn will dispose with data reader
	SqlConnection conn = await GetConnection();
	using var cmd = conn.CreateCommand();
	cmd.CommandText = "select Date, TempCelsius, Summary from forecast";
	return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
}
```

The standard JSON formatter doesn't have any specific knowledge of `DbDataReader`, and treats it as a simple `IEnumerable` 
where the only exposed property is `FieldCount`, so the default formatter produces a result such as the following.

```JSON
[{"fieldCount":3},{"fieldCount":3},{"fieldCount":3},{"fieldCount":3},{"fieldCount":3},{"fieldCount":3}]
```

With the Sylvan `JsonDataOutputFormatter`, the result would look like the following:

```JSON
[{"Date":"2022-01-01T00:00:00","TempCelsius":12,"Summary":"warm"},{"Date":"2022-01-02T00:00:00","TempCelsius":13,"Summary":"breezy"},{"Date":"2022-01-03T00:00:00","TempCelsius":23,"Summary":"comfy"},{"Date":"2022-01-04T00:00:00","TempCelsius":38,"Summary":"balmy"},{"Date":"2022-01-05T00:00:00","TempCelsius":2,"Summary":"fridgid"},{"Date":"2022-01-06T00:00:00","TempCelsius":15,"Summary":"cool"}]
```


# Performance

The performance of the data formatter is comparable to the standard ASP.NET JSON formatter as it uses the same `Utf8JsonWriter` internally.