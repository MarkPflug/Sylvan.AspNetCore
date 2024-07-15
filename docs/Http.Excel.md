# Sylvan.AspNetCore.Mvc.Excel

This package provides support for Excel responses in ASP.NET Core minimal APIs.

## ExcelResult

The output formatter can handle API methods that return `IDataReader`, `DbDataReader`, or `IEnumerable<T>` where T is some complex object.


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
