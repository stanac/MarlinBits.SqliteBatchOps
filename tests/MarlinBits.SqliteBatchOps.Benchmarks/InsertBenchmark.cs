using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.Sqlite;

namespace MarlinBits.SqliteBatchOps.Benchmarks;

[SimpleJob]
public class InsertBenchmark
{
    private const string RamDiskDbFileDir = @"r:\dbtests\";
    private const string NvmeDiskDbFileDir = @"e:\dbtests\";
    private string? _dbFilePath;
    private SqliteConnection? _connection;
    private BatchOps? _batchOps;

    private const string CreateTableSql = "CREATE TABLE T1 (Val1 INTEGER NOT NULL)";
    private const string InsertSql = "INSERT INTO T1 (Val1) VALUES (@value)";

    private object GetParam(int value) => new { value };

    private string GetNewConnectionString()
    {
        // string dir = DiskType == "RAM" ? RamDiskDbFileDir : NvmeDiskDbFileDir;
        string dir =NvmeDiskDbFileDir;

        string fileName = string.Format("testdb-{0}.sqlite", Guid.NewGuid().ToString("N"));
        _dbFilePath = Path.Combine(dir, fileName);

        string connectionString = "Data Source=" + _dbFilePath;

        return connectionString;
    }

    [Params(1, 5 /*, 10, 25, 50, 100, 250, 500, 1_000*/)] public int NumberOfConcurrentInserts;
    // [Params(true, false)] public bool UseWriteAheadLogging;
    // [Params("RAM", "NVMe")] public string DiskType = "";

    [IterationSetup]
    public void Setup()
    {
        string connectionString = GetNewConnectionString();
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        
        if (/*UseWriteAheadLogging*/ true)
        {
            _connection.Execute("PRAGMA journal_mode = WAL");
        }

        _connection.Execute(CreateTableSql);

        _batchOps = new(connectionString, new BatchOpsSettings
        {
            // UseWriteAheadLogging = UseWriteAheadLogging
            UseWriteAheadLogging = true
        });
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
        IEnumerable<Task<int>> tasks = Enumerable.Range(0, NumberOfConcurrentInserts)
            .Select(value => _connection!.ExecuteAsync(InsertSql, GetParam(value)));

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task InsertWithBatch()
    {
        IEnumerable<Task<long?>> tasks = Enumerable.Range(0, NumberOfConcurrentInserts)
            .Select(value => _batchOps!.ExecuteAsync(InsertSql, GetParam(value)));

        await Task.WhenAll(tasks);
    }
}