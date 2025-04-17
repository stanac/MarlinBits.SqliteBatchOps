using FluentAssertions;

namespace SqliteBatchOps.Tests;

public class UnitTest1 : IDisposable
{
    private const string CreateTableSql =
        """
        CREATE TABLE T1 (Val1 INTEGER, Val2 TEXT NOT NULL)
        """;
    private readonly TestDb _testDb = new(CreateTableSql);

    [Fact]
    public async Task SingleInsert_GetCount_Returns1()
    {
        using BatchOpsFactory factory = new();
        BatchOps batchOps = factory.GetBatchOps(_testDb.DbConnectionString);

        await batchOps.ExecuteAsync("INSERT INTO T1 (Val1, Val2) VALUES (1, '1')");

        int count = _testDb.QueryFirstOrDefault<int>("SELECT COUNT(1) FROM T1");

        count.Should().Be(1);
    }

    [Fact]
    public async Task MultipleInserts_GetCount_ReturnsExpectedCount()
    {
        const int countToInsert = 10_120;

        using BatchOpsFactory factory = new();
        BatchOps batchOps = factory.GetBatchOps(_testDb.DbConnectionString);

        IEnumerable<Task> tasks = Enumerable.Range(0, countToInsert)
            .Select(value => batchOps.ExecuteAsync("INSERT INTO T1 (Val1, Val2) VALUES (@v1, @v2)", new
            {
                v1 = value,
                v2 = value.ToString()
            }));

        await Task.WhenAll(tasks);

        int count = _testDb.QueryFirstOrDefault<int>("SELECT COUNT(1) FROM T1");

        count.Should().Be(countToInsert);
    }

    public void Dispose()
    {
        _testDb.Dispose();
    }
}
