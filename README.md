# SqliteBatchOps

Fast batch insert/update/delete for SQLite with Dapper.

## What?

SqliteBatchOps is adding extension method to IDbConnection and is using [Dapper](https://github.com/DapperLib/Dapper) to execute SQL statements in single transaction for faster execution.

## Why?

SQLite is very slow with opening and closing transactions. 
You can perform 200 inserts without transaction or 50000 with a single transaction.

If you don't specify a transaction SQLite will use new transaction for each SQLite statement. 
Since SQLite can have only one writer, this becomes a bottleneck.

This library is using Dapper to transparently execute modifying operations in batch
to speed up inserts, updates and deletes.

## How to use it?

Reference nuget package:

```
dotnet add package Dapper SqliteBatchOps
```

```
// other usings
using SqliteBatchOps;


using var connection = new SqliteConnection("Data Source=db.sqlite");

connection.

```

## How it works?