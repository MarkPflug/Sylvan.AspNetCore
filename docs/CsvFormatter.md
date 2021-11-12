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
The CSV formatter exceeds the performance of JSON for every configuration.

### Input formatter

This benchmark measures the time taken to make 100 sequential HTTP requests with a payload of `RecordCount` records, which then return the average `TempC` for the posted data.
The CSV input formatter outperforms the standard Json formatter, and the 
performance difference increases as the payload size increases.

`Baseline` measures the overhead of making a API request that returns a constant integer value.

`Json` measures the standard `application/json` handler binding to objects.

`Csv` measures the Sylvan 'text/csv' formatter binding to objects.

`CsvData` measures the Sylvan 'text/csv' formatter binding as a `DbDataReader`.
This is faster than binding to objects as only the `TemperatureC` column needs to be accessed.

|   Method | RecordCount |       Mean |     Error |    StdDev |     Gen 0 | Allocated |
|--------- |------------ |-----------:|----------:|----------:|----------:|----------:|
| Baseline |          10 |   9.168 ms | 3.1326 ms | 0.4848 ms |         - |      1 MB |
|     Json |          10 |  14.278 ms | 2.4900 ms | 0.3853 ms |         - |      2 MB |
|      Csv |          10 |  14.019 ms | 0.6350 ms | 0.0983 ms | 1000.0000 |      5 MB |
|  CsvData |          10 |  13.415 ms | 3.5817 ms | 0.5543 ms | 1000.0000 |      5 MB |
| Baseline |         100 |   8.188 ms | 0.5791 ms | 0.0317 ms |         - |      1 MB |
|     Json |         100 |  25.411 ms | 0.8313 ms | 0.1286 ms |         - |      3 MB |
|      Csv |         100 |  15.930 ms | 3.2947 ms | 0.5099 ms | 1000.0000 |      6 MB |
|  CsvData |         100 |  14.209 ms | 1.1780 ms | 0.1823 ms | 1000.0000 |      5 MB |
| Baseline |        1000 |   8.310 ms | 1.2671 ms | 0.1961 ms |         - |      1 MB |
|     Json |        1000 | 145.833 ms | 5.2579 ms | 0.8137 ms | 3000.0000 |     13 MB |
|      Csv |        1000 |  36.995 ms | 8.9444 ms | 1.3842 ms | 3000.0000 |     15 MB |
|  CsvData |        1000 |  25.637 ms | 2.9596 ms | 0.4580 ms | 1000.0000 |      5 MB |

### Output formatter

This benchmark measures the time taken to make 100 HTTP requests which return a payload of `RecordCount` WeatherForecast records.

| Method | RecordCount |     Mean |    Error |   StdDev |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|------- |------------ |---------:|---------:|---------:|----------:|----------:|----------:|----------:|
|   Json |          10 | 11.32 ms | 0.060 ms | 0.031 ms |  400.0000 |         - |         - |      2 MB |
|    Csv |          10 | 10.80 ms | 0.066 ms | 0.035 ms |  400.0000 |         - |         - |      2 MB |
|   Json |         100 | 17.65 ms | 0.039 ms | 0.017 ms | 1000.0000 |         - |         - |      4 MB |
|    Csv |         100 | 13.48 ms | 0.047 ms | 0.024 ms |  600.0000 |         - |         - |      3 MB |
|   Json |        1000 | 95.84 ms | 0.512 ms | 0.227 ms | 9000.0000 | 9000.0000 | 9000.0000 |     40 MB |
|    Csv |        1000 | 40.72 ms | 0.096 ms | 0.050 ms | 3200.0000 |  200.0000 |         - |     13 MB |
