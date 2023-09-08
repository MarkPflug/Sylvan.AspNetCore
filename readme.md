# <img src="Sylvan.png" height="48" alt="Sylvan Logo"/> Sylvan.AspNetCore

This repository is a sibling to the [Sylvan](https://github.com/MarkPflug/Sylvan) repository and is home to libraries specific to ASP.NET Core. The packages here build upon the [Sylvan.Data.Csv](https://github.com/MarkPflug/Sylvan) and [Sylvan.Data.Excel](https://github.com/MarkPflug/Sylvan.Data.Excel) packages to offer extremely efficient, and fully async implementations.

## [Sylvan.AspNetCore.Mvc.Csv](docs/Mvc.Csv.md)

A library that implements `text/csv` content negotiation for ASP.NET Core MVC APIs.

## [Sylvan.AspNetCore.Mvc.Excel](docs/Mvc.Excel.md)

A library that implements Excel content negotiation for ASP.NET Core MVC APIs.

## [Sylvan.AspNetCore.Http.Csv](docs/Http.Csv.md)

Provides a `CsvResult` type for ASP.NET minimal apis.

## [Sylvan.AspNetCore.Http.Excel](docs/Http.Excel.md)

Provides an `ExcelResult` type for ASP.NET minimal apis.

## [Sylvan.AspNetCore.Mvc.JsonData](docs/Mvc.JsonData.md)

Provides a JSON formatter to handle un-typed DbDataReader responses.