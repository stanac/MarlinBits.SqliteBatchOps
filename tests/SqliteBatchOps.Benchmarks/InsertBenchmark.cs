using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.Sqlite;

namespace SqliteBatchOps.Benchmarks;

[SimpleJob]
public class InsertBenchmark
{
    private const string DbFileDir = @"r:\dbtests\";
    private string? _dbFilePath;
    private SqliteConnection? _connection;
    private BatchOps? _batchOps;

    private const string CreateTableSql = "CREATE TABLE T1 (Val1 INTEGER NOT NULL)";
    private const string InsertSql = "INSERT INTO T1 (Val1) VALUES (@value)";

    private object GetParam(int value) => new { value };

    private string GetNewConnectionString()
    {
        string fileName = string.Format("testdb-{0}.sqlite", Guid.NewGuid().ToString("N"));
        _dbFilePath = Path.Combine(DbFileDir, fileName);

        string connectionString = "Data Source=" + _dbFilePath;

        return connectionString;
    }

    [Params(10, 100, 1_000, 10_000)] public int Count;
    // [Params(true, false)] public bool UseWal;

    [IterationSetup]
    public void Setup()
    {
        string connectionString = GetNewConnectionString();
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        _connection.Execute(CreateTableSql);

        _batchOps = new(connectionString);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();

            SqliteConnection.ClearPool(_connection);

            try
            {
                File.Delete(_dbFilePath!);
            }
            catch
            {
                // nop
            }
        }
    }

    [Benchmark]
    public async Task InsertsWithoutBatch()
    {
        IEnumerable<Task<int>> tasks = Enumerable.Range(0, Count)
            .Select(value => _connection!.ExecuteAsync(InsertSql, GetParam(value)));

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task InsertWithBatch()
    {
        IEnumerable<Task<long?>> tasks = Enumerable.Range(0, Count)
            .Select(value => _batchOps!.ExecuteAsync(InsertSql, GetParam(value)));

        await Task.WhenAll(tasks);
    }
}