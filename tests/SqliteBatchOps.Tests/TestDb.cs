using Dapper;
using Microsoft.Data.Sqlite;

namespace SqliteBatchOps.Tests;

public class TestDb : IDisposable
{
    private readonly string _parentDbsPath;
    private readonly string _dbTestRootDirPath;
    public string DbConnectionString { get; }

    public TestDb(string? createTableSql = null, bool useWal = false)
    {
        _parentDbsPath = Environment.GetEnvironmentVariable("DbTestDir")
                         ?? Directory.CreateTempSubdirectory("testDbDir").FullName;

        _dbTestRootDirPath = Path.Combine(_parentDbsPath, $"TestDb-{DateTimeOffset.UtcNow:yyyy-MM-dd-HH-mm-ss}-{Guid.NewGuid():N}");
        
        if (!Directory.Exists(_dbTestRootDirPath))
        {
            Directory.CreateDirectory(_dbTestRootDirPath);
        }

        DbConnectionString = $"Data Source={Path.Combine(_dbTestRootDirPath, "test-db.sqlite")}";

        if (useWal)
        {
            Execute("PRAGMA journal_mode = WAL");
        }
        
        if (createTableSql != null)
        {
            Execute(createTableSql);
        }
    }

    public void Execute(string sql)
    {
        SqliteConnection c = new SqliteConnection(DbConnectionString);
        c.Open();

        c.Execute(sql);

        c.Dispose();
        SqliteConnection.ClearAllPools();
    }

    public T? QueryFirstOrDefault<T>(string sql)
    {
        SqliteConnection c = new SqliteConnection(DbConnectionString);
        c.Open();

        T? result = c.QueryFirstOrDefault<T>(sql);

        c.Dispose();
        SqliteConnection.ClearAllPools();

        return result;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_dbTestRootDirPath, true);
        }
        catch
        {
            Console.WriteLine("Failed to delete test DB");
        }

        // try to clean up for old finished tests
        foreach (string dir in Directory.GetDirectories(_parentDbsPath))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // nop
            }
        }
    }
}