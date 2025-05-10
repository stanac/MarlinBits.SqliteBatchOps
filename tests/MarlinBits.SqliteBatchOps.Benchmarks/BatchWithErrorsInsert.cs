using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.Sqlite;

namespace MarlinBits.SqliteBatchOps.Benchmarks;

public class BatchWithErrorsInsert
{
    private const string NvmeDiskDbFileDir = @"e:\dbtests\";
    private string? _dbFilePath;
    private SqliteConnection? _connection;
    private BatchOps? _batchOps;

    private const string CreateTableSql = "CREATE TABLE T1 (Val1 INTEGER NOT NULL)";
    private const string InsertSql = "INSERT INTO T1 (Val1) VALUES (@value)";
    private const string InsertSqlError = "INSERT INTO T2 (Val1) VALUES (@value)";

    private readonly HashSet<int> _errorIndices = [];

    private object GetParam(int value) => new { value };

    private string GetNewConnectionString()
    {
        // string dir = DiskType == "RAM" ? RamDiskDbFileDir : NvmeDiskDbFileDir;
        string dir = NvmeDiskDbFileDir;

        string fileName = string.Format("testdb-{0}.sqlite", Guid.NewGuid().ToString("N"));
        _dbFilePath = Path.Combine(dir, fileName);

        string connectionString = "Data Source=" + _dbFilePath;

        return connectionString;
    }

    [Params(50, 100, 1_000, 10_000)] 
    public int NumberOfConcurrentInserts;

    [Params(0, 1, 2)]
    public int PercentOfErrorStatements;

    private bool IsError(int index)
    {
        return _errorIndices.Contains(index);
    }

    private string GetSql(int index) => IsError(index) ? InsertSqlError : InsertSql;

    [IterationSetup]
    public void Setup()
    {
        string connectionString = GetNewConnectionString();
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        _connection.Execute("PRAGMA journal_mode = WAL");

        _connection.Execute(CreateTableSql);

        _batchOps = new(connectionString, new BatchOpsSettings
        {
            UseWriteAheadLogging = true
        });

        Random rng = new();
        _errorIndices.Clear();
        int errorCount = (int)(NumberOfConcurrentInserts * PercentOfErrorStatements / 100.0);

        while (_errorIndices.Count < errorCount)
        {
            _errorIndices.Add(rng.Next(NumberOfConcurrentInserts));
        }
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
    public async Task InsertWithBatch()
    {
        IEnumerable<Task<long?>> tasks = Enumerable.Range(0, NumberOfConcurrentInserts)
            .Select(value => _batchOps!.ExecuteAsync(GetSql(value), GetParam(value)));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // nop
        }
    }
}