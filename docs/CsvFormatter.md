# Sylvan.AspNetCore.Mvc.Formatters.Csv

This package provides support for CSV content negotiation for handling tabular data in ASP.NET Core web APIs.
It allows a client to choose to send and receive CSV data by specifying `text/csv` for the `ContentType` or `Accept` HTTP headers.

JSON is a great format for serializing structured, heirarchical data. 
However, for purely rectangular/tabular data, JSON can become quite verbose as each property name must be repeated for each record in the dataset. CSV, on the other hand, only names each column once at the beginning of the output. This can greatly reduce the size of the payload if the dataset is large, while remaining relatively easy to process by most clients.

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

## Benchmarks

These benchmarks compare the performance of the Sylvan CSV formatter to the standard JSON formatter.
The CSV formatter matches or exceeds the performance of JSON for every configuration.

### Input formatter

This benchmark measures the time taken to make 100 sequential HTTP requests with a payload of `RecordCount` records, which then return the average `TempC` for the posted data.
The CSV input formatter outperforms the standard Json formatter, and the 
performance difference increases as the payload size increases.

`Baseline` measures the overhead of making a API request that returns a constant integer value.

`Json` measures the standard `application/json` handler binding to objects.

`Csv` measures the Sylvan 'text/csv' formatter binding to objects.

`CsvData` measures the Sylvan 'text/csv' formatter binding as a `DbDataReader`.


|    Method | RecordCount |       Mean |     Error |    StdDev | Allocated |
|---------- |------------ |-----------:|----------:|----------:|----------:|
|  Baseline |          10 |   3.222 ms | 0.4202 ms | 0.1091 ms |   1.23 MB |
|      Json |          10 |   8.309 ms | 0.8043 ms | 0.2868 ms |   1.67 MB |
|       Csv |          10 |   7.174 ms | 0.1040 ms | 0.0270 ms |    5.5 MB |
|   CsvData |          10 |   6.830 ms | 0.1003 ms | 0.0055 ms |   5.38 MB |
|  Baseline |         100 |   3.183 ms | 0.0389 ms | 0.0060 ms |   1.23 MB |
|      Json |         100 |  15.375 ms | 0.3709 ms | 0.0574 ms |   2.69 MB |
|       Csv |         100 |  12.921 ms | 0.6605 ms | 0.2355 ms |   6.37 MB |
|   CsvData |         100 |   9.467 ms | 0.0987 ms | 0.0153 ms |   5.43 MB |
|  Baseline |        1000 |   3.204 ms | 0.0446 ms | 0.0069 ms |   1.23 MB |
|      Json |        1000 |  91.450 ms | 1.7149 ms | 0.4454 ms |  12.59 MB |
|       Csv |        1000 |  44.628 ms | 1.2516 ms | 0.4463 ms |   15.3 MB |
|   CsvData |        1000 |  33.539 ms | 0.5211 ms | 0.0806 ms |   6.11 MB |

### Output formatter

This benchmark measures the time taken to make 100 HTTP requests which return a payload of `RecordCount` WeatherForecast records.

|  Method | RecordCount |       Mean |      Error |    StdDev | Allocated |
|-------- |------------ |-----------:|-----------:|----------:|----------:|
|    Json |          10 |   6.974 ms |  0.3353 ms | 0.0871 ms |   1.75 MB |
|     Csv |          10 |   7.275 ms |  0.6244 ms | 0.2227 ms |   1.66 MB |
| CsvData |          10 |   7.373 ms |  0.4829 ms | 0.1722 ms |   1.67 MB |
|    Json |         100 |  11.097 ms |  0.1228 ms | 0.0067 ms |   4.15 MB |
|     Csv |         100 |  10.075 ms |  0.1573 ms | 0.0408 ms |   2.68 MB |
| CsvData |         100 |  10.592 ms |  1.0436 ms | 0.3722 ms |   2.68 MB |
|    Json |        1000 |  58.032 ms |  6.5122 ms | 1.6912 ms |  40.52 MB |
|     Csv |        1000 |  42.816 ms |  3.0557 ms | 1.0897 ms |   14.5 MB |
| CsvData |        1000 |  41.228 ms |  0.6327 ms | 0.0979 ms |   14.5 MB |
