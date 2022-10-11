# Sylvan.AspNetCore.Mvc.Formatters.Csv

This package provides support for CSV content negotiation for handling tabular data in ASP.NET Core web APIs.
It allows a client to choose to send and receive CSV data by specifying `text/csv` for the `ContentType` or `Accept` HTTP headers.

JSON is a great format for serializing structured, heirarchical data. 
However, for purely rectangular, tabular data, JSON can become quite verbose as each property name must be repeated for each record in the dataset. CSV, on the other hand, only names each column once at the beginning of the output. This can greatly reduce the size of the payload if the dataset is large, while remaining relatively easy to process by most clients.

Using the WeatherForecast example that is delivered with the ASP.NET project template, with only 4 records returned the size of the payload is nearly halved by using CSV instead of JSON. The size reduction becomes even more significant as the size of the record set increases.

The default JSON response producing 367 bytes.

```JSON
[{"date":"2021-05-21T00:00:00-07:00","temperatureC":-2,"temperatureF":29,"summary":"Bracing"},{"date":"2021-05-22T00:00:00-07:00","temperatureC":15,"temperatureF":58,"summary":"Hot"},{"date":"2021-05-23T00:00:00-07:00","temperatureC":29,"temperatureF":84,"summary":"Mild"},{"date":"2021-05-24T00:00:00-07:00","temperatureC":6,"temperatureF":42,"summary":"Scorching"}]
```

The CSV response produces only 198 bytes for the same data.

```CSV
Date,TemperatureC,TemperatureF,Summary
2021-05-21T00:00:00-07:00,-2,29,Bracing
2021-05-22T00:00:00-07:00,15,58,Hot
2021-05-23T00:00:00-07:00,29,84,Mild
2021-05-24T00:00:00-07:00,6,42,Scorching
```

## InputFormatter

The input formatter can handle APIs that accept `IDataReader`, `DbDataReader`, or a type that implements `IEnumerable<T>` where T is some complex object. The default behavior will automatically detect a delimiter from the following list `,`, `\t`, `;`, or `|`, and expect a header row to be present. The client can specify a different delimiter by setting the Csv-Delimiter HTTP header on the request. The client can indicate that there are no headers by setting Csv-HasHeaders HTTP header to "false" or "0".

While IDataReader can be used as a parameter, using DbDataReader should be preferred. The IDataReader interface does not expose asynchronous operations, while the DbDataReader abstract class does.

## OutputFormatter

The output formatter can handle API methods that return `IDataReader`, `DbDataReader`, or `IEnumerable<T>` where T is some complex object.

## Usage

To enable the CSV formatter, add the `Sylvan.AspNetCore.Mvc.Formatters.Csv` nuget package to your project. This package transitively depends on `Sylvan.Data.Csv` for CSV processing and `Sylvan.Data` for binding to and from objects.

Register the CSV formatter with the MVC service in your application `ConfigureServices`.

```C#
services.AddControllers(opts => { opts.AddSylvanCsvFormatters(); });
```

This will register both the `CsvInputFormatter` as well as the `CsvOutputFormatter`. If you only want one or the other, they can be registered individually.

```C#
services.AddControllers(
    opts =>
    {
        opts.InputFormatters.Add(new CsvInputFormatter());
        opts.OutputFormatters.Add(new CsvOutputFormatter());
    }
);
```
