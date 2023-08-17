# Sylvan.AspNetCore.Mvc.Excel

This package provides support for Excel file responses and content negotiation for handling tabular data in ASP.NET Core web APIs.
It allows a client to choose to send and receive Excel data by specifying the appropriate `ContentType` or `Accept` HTTP headers. This library uses `Sylvan.Data.Excel` to provide an extremely efficient of Excel processing.

## InputFormatter

The input formatter can handle APIs that accept `IDataReader`, `DbDataReader`, or a type that implements `IEnumerable<T>` where T is some complex object. The input formatter can accept the content-types listed in the following table.

| File-Extension | Content-Type |
|-|-|
|`.xlsx`|`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`|
|`.xlsb`|`application/vnd.ms-excel.sheet.binary.macroEnabled.12`|
|`.xls`|`application/vnd.ms-excel`|

## OutputFormatter

The output formatter can handle API methods that return `IDataReader`, `DbDataReader`, or `IEnumerable<T>` where T is some complex object.

The output formatter supports the .xlsx and .xlsb content-types for the `Accept` header.

## Usage

To enable the Excel formatter, add the `Sylvan.AspNetCore.Mvc.Formatters.Excel` nuget package to your project. This package transitively depends on `Sylvan.Data.Excel` for Excel processing and `Sylvan.Data` for binding to and from objects.

Register the Excel formatter with the MVC service in your application `ConfigureServices`.

```C#
services.AddControllers(opts => { opts.AddSylvanExcelFormatters(); });
```

This will register both the `ExcelInputFormatter` as well as the `ExcelOutputFormatter`. If you only want one or the other, they can be registered individually.

```C#
services.AddControllers(
    opts =>
    {
        opts.InputFormatters.Add(new ExcelInputFormatter());
        opts.OutputFormatters.Add(new ExcelOutputFormatter());
    }
);
```
