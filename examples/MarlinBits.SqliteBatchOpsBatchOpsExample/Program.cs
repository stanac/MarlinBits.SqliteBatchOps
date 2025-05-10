using Dapper;
using MarlinBits.SqliteBatchOps;
using Microsoft.Data.Sqlite;

namespace MarlinBits.SqliteBatchOpsBatchOpsExample;

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