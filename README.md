# Marlin.BatchOps

Fast batch insert/update/delete for `Microsoft.Data.Sqlite`.

This library can execute 30x more commands (inserts/deletes/updates) per second compared to execution of individual commands. But it has ~55ms overhead for individual commands.

## What?

`Marlin.BatchOps` is has batch command executor that is using [Dapper](https://github.com/DapperLib/Dapper) to execute SQL statements in single transaction for faster execution.

## Why?

SQLite is very slow with opening and closing transactions. 
You can perform 200 inserts per second without transaction or hundreds of thousands of inserts per second with a few transactions.

If you don't specify a transaction SQLite will use new transaction for each SQL write statement (including deletes and updates). 
Since SQLite can have only one writer, it is a bottleneck.

This library is using Dapper to transparently execute modifying operations in batch
to speed up inserts, updates and deletes.

## How it works?

For each SQLite db your application use, you can create an instance of `BatchOps`.
It will use a thread-synced queue in the background and trigger writes every 50,000 operations (configurable) or every 50 ms (configurable), whichever comes first.

Use `BatchOpsFactory` to ensure single instance of `BatchOps` per database/connection string.

## How to use it?

Reference nuget package:

```
dotnet add package Marlin.BatchOps
```

```csharp
using Dapper;
using Marlin.BatchOps;
using Microsoft.Data.Sqlite;

namespace MarlinBatchOpsExample;

public class Program
{
    private const string ConnectionString = "Data Source=my-db.sqlite";
    private readonly BatchOpsFactory _factory = new(); //!!! Register as singleton in web app
    private readonly BatchOps _batchOps;

    // Even though it makes no sense to use BatchOps in single client mode
    // this code shows basic functions of the library

    public Program()
    {
        _factory.SetSettingsForBatchOps(ConnectionString, new BatchOpsSettings
        {
            UseWriteAheadLogging = true
        });

        _batchOps = _factory.GetBatchOps(ConnectionString);
    }

    public static async Task Main()
    {
        Program p = new();
        await p.RunAsync();
    }

    private async Task RunAsync()
    {
        CreateTable();

        await _batchOps.ExecuteAsync("INSERT INTO T1(Val1, Val2) VALUES (@val1, @val2)", new
        {
            val1 = 1,
            val2 = "1"
        });

        await _batchOps.ExecuteAsync("INSERT INTO T1(Val1, Val2) VALUES (@val1, @val2)", new
        {
            val1 = 2,
            val2 = "2"
        });

        await using SqliteConnection c = new(ConnectionString);

        int count = c.QuerySingle<int>("SELECT COUNT(1) FROM T1");

        Console.WriteLine($"T1 row count: {count}");
    }

    private void CreateTable()
    {
        const string sqlCreateTable =
            """
            CREATE TABLE IF NOT EXISTS T1 (Val1 INTEGER, Val2 TEXT)
            """;

        using SqliteConnection c = new(ConnectionString);
        c.Execute("PRAGMA journal_mode = WAL");
        c.Execute(sqlCreateTable);

        Console.WriteLine("Table created (if it didn't exist already)");
    }
}
```

## Downsides

This library has an overhead of ~50 ms by default, it can be lowered to ~25 ms. 
It isn't suitable for single client applications (mobile, desktop).
For those types of application use regular transactions when inserting/deleting/updating in batch.

It is highly recommended to use it in WAL mode (see `BatchOpsSettings`).

Only `async` operations are supported.
`Task`s are used as callback functions and there is no way to achieve the same with sync operation.

SQL statements with errors will slow down other statements (see last four benchmark tables).
This shouldn't be a real life problem if application is tested properly.

## Benchmarks using NVMe drive and WAL mode

```SQL
INSERT INTO T1 (Val1 /*INTEGER*/, Val2 /*TEXT*/) VALUES (@val1, @val2)
```

| Method              | NumberOfConcurrentInserts | Mean         | Error      | StdDev     |
|-------------------- |-------------------------- |-------------:|-----------:|-----------:|
| InsertsWithoutBatch | 1                         |     2.159 ms |  0.0429 ms |  0.0459 ms |
| InsertWithBatch     | 1                         |    55.974 ms |  1.0076 ms |  0.9425 ms |
| InsertsWithoutBatch | 5                         |     9.963 ms |  0.1585 ms |  0.1483 ms |
| InsertWithBatch     | 5                         |    56.269 ms |  1.0900 ms |  1.1194 ms |
| InsertsWithoutBatch | 10                        |    19.503 ms |  0.2107 ms |  0.1971 ms |
| InsertWithBatch     | 10                        |    55.478 ms |  0.7320 ms |  0.6489 ms |
| InsertsWithoutBatch | 25                        |    47.965 ms |  0.7238 ms |  0.6770 ms |
| InsertWithBatch     | 25                        |    55.638 ms |  1.0796 ms |  1.0603 ms |
| InsertsWithoutBatch | 50                        |    95.743 ms |  1.0973 ms |  0.9727 ms |
| InsertWithBatch     | 50                        |    58.214 ms |  1.1541 ms |  1.9597 ms |
| InsertsWithoutBatch | 100                       |   192.601 ms |  2.3555 ms |  2.2034 ms |
| InsertWithBatch     | 100                       |    58.518 ms |  1.1661 ms |  1.8496 ms |
| InsertsWithoutBatch | 250                       |   478.653 ms |  6.2588 ms |  5.2264 ms |
| InsertWithBatch     | 250                       |    56.195 ms |  1.0941 ms |  1.1707 ms |
| InsertsWithoutBatch | 500                       |   947.968 ms |  9.7244 ms |  8.1203 ms |
| InsertWithBatch     | 500                       |    56.001 ms |  1.0360 ms |  1.0175 ms |
| InsertsWithoutBatch | 1000                      | 1,913.983 ms | 15.8844 ms | 13.2642 ms |
| InsertWithBatch     | 1000                      |    55.692 ms |  0.6424 ms |  0.8575 ms |

Performance gains:

| Inserts | Without Batch | With Batch | Performance Gain |
|---------|-------------- |----------- |----------------- |
| 1       |    2 ms       | 57 ms      |     0.035x       |
| 5       |    9 ms       | 57 ms      |     0.15x        |
| 10      |   19 ms       | 57 ms      |     0.33x        |
| 25      |   47 ms       | 57 ms      |     0.82x        |
| 50      |   95 ms       | 59 ms      |     1.62x        |
| 100     |  192 ms       | 59 ms      |     3.25x        |
| 250     |  478 ms       | 59 ms      |     8.1x         |
| 500     |  947 ms       | 59 ms      |     16x          |
| 1000    | 1913 ms       | 59 ms      |     32x          |

Batch operations take consistently around 60ms, better results may be achieved with different `BatchOpsSettings` (`MillisecondsWait` property, default value is 50).

Below are results of batch inserts only, because running more than 1000 discrete inserts is very slow (even in WAL mode).

`MillisecondsWait` property set to 50:

| Method          | NumberOfConcurrentInserts | Mean      | Error    | StdDev    |
|---------------- |-------------------------- |----------:|---------:|----------:|
| InsertWithBatch | 1                         |  57.77 ms | 1.150 ms |  1.953 ms |
| InsertWithBatch | 5                         |  58.06 ms | 1.127 ms |  1.852 ms |
| InsertWithBatch | 10                        |  58.16 ms | 1.161 ms |  1.808 ms |
| InsertWithBatch | 25                        |  58.29 ms | 1.158 ms |  1.837 ms |
| InsertWithBatch | 50                        |  56.10 ms | 1.058 ms |  1.133 ms |
| InsertWithBatch | 100                       |  56.02 ms | 0.964 ms |  0.854 ms |
| InsertWithBatch | 250                       |  55.82 ms | 0.971 ms |  0.909 ms |
| InsertWithBatch | 500                       |  55.73 ms | 0.709 ms |  0.663 ms |
| InsertWithBatch | 1,000                     |  55.40 ms | 1.079 ms |  1.108 ms |
| InsertWithBatch | 10,000                    |  67.87 ms | 1.289 ms |  1.484 ms |
| InsertWithBatch | 25,000                    | 114.31 ms | 4.285 ms | 12.500 ms |
| InsertWithBatch | 50,000                    | 170.42 ms | 3.340 ms |  6.354 ms |
| InsertWithBatch | 100,000                   | 317.20 ms | 4.165 ms |  3.692 ms |

`MillisecondsWait` property set to 10:

| Method          | NumberOfConcurrentInserts | Mean      | Error    | StdDev   |
|---------------- |-------------------------- |----------:|---------:|---------:|
| InsertWithBatch | 1                         |  23.72 ms | 0.474 ms | 1.281 ms |
| InsertWithBatch | 5                         |  24.09 ms | 0.928 ms | 2.723 ms |
| InsertWithBatch | 10                        |  24.26 ms | 0.798 ms | 2.342 ms |
| InsertWithBatch | 25                        |  24.85 ms | 0.479 ms | 0.656 ms |
| InsertWithBatch | 50                        |  24.60 ms | 0.732 ms | 2.148 ms |
| InsertWithBatch | 100                       |  24.24 ms | 0.484 ms | 1.412 ms |
| InsertWithBatch | 250                       |  24.30 ms | 0.485 ms | 1.064 ms |
| InsertWithBatch | 500                       |  24.66 ms | 0.484 ms | 0.739 ms |
| InsertWithBatch | 1000                      |  23.62 ms | 0.543 ms | 1.592 ms |
| InsertWithBatch | 10000                     |  34.92 ms | 0.772 ms | 2.239 ms |
| InsertWithBatch | 25000                     |  81.71 ms | 1.587 ms | 2.173 ms |
| InsertWithBatch | 50000                     | 163.75 ms | 3.245 ms | 4.104 ms |
| InsertWithBatch | 100000                    | 328.61 ms | 4.892 ms | 4.085 ms |

Somewhat better results are achieved with lowering wait time, but not significantly.

Percentage of error statements and how it affect performance:

| Method          | NumberOfConcurrentInserts | PercentOfErrorStatements | Mean        | Error     | StdDev    |
|---------------- |-------------------------- |------------------------- |------------:|----------:|----------:|
| InsertWithBatch | 50                        | 0                        |    57.84 ms |  1.135 ms |  1.957 ms |
| InsertWithBatch | 50                        | 1                        |    57.79 ms |  1.152 ms |  2.192 ms |
| InsertWithBatch | 50                        | 2                        |    55.83 ms |  1.116 ms |  1.096 ms |

| Method          | NumberOfConcurrentInserts | PercentOfErrorStatements | Mean        | Error     | StdDev    |
|---------------- |-------------------------- |------------------------- |------------:|----------:|----------:|
| InsertWithBatch | 100                       | 0                        |    56.30 ms |  0.701 ms |  0.656 ms |
| InsertWithBatch | 100                       | 1                        |    55.75 ms |  1.071 ms |  1.099 ms |
| InsertWithBatch | 100                       | 2                        |    55.43 ms |  1.050 ms |  1.124 ms |

| Method          | NumberOfConcurrentInserts | PercentOfErrorStatements | Mean        | Error     | StdDev    |
|---------------- |-------------------------- |------------------------- |------------:|----------:|----------:|
| InsertWithBatch | 1000                      | 0                        |    55.45 ms |  0.794 ms |  0.883 ms |
| InsertWithBatch | 1000                      | 1                        |    69.55 ms |  1.377 ms |  2.718 ms |
| InsertWithBatch | 1000                      | 2                        |    82.90 ms |  1.648 ms |  4.371 ms |

| Method          | NumberOfConcurrentInserts | PercentOfErrorStatements | Mean        | Error     | StdDev    |
|---------------- |-------------------------- |------------------------- |------------:|----------:|----------:|
| InsertWithBatch | 10000                     | 0                        |    66.68 ms |  1.244 ms |  2.009 ms |
| InsertWithBatch | 10000                     | 1                        |   619.73 ms | 12.359 ms | 32.124 ms |
| InsertWithBatch | 10000                     | 2                        | 1,165.97 ms | 22.958 ms | 47.922 ms |

All benchmarks are executed using BenchmarkDotNet

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3775)
AMD Ryzen 5 5600X, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.201
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  Job-VOMVFZ : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
```

## When not to use the library

Looking at the tables above it is clear that this library should not be used for databases with
low number of concurrent commands. 
In other words, don't use it for single client applications (like mobile or desktop applications).
Also this library is not suitable for internal web applications with low number of user, or low number of writes.

## When to use the library

If you are developing web application (SSR/API) with burst write loads (where you expect to exceed more than 50 writes per second)
I would recommend to use the library, and only in WAL mode.