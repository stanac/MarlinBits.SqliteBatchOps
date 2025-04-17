using Dapper;
using Microsoft.Data.Sqlite;

namespace SqliteBatchOps;

public class BatchOps : IDisposable
{
    private readonly SqliteConnection _connection;
    private const string WalPragma = "PRAGMA journal_mode = WAL";
    private readonly CommandQueue _queue;

    public BatchOps(string connectionString) : this(connectionString, new())
    {
    }

    public BatchOps(string connectionString, BatchOpsSettings settings)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        if (settings.UseWriteAheadLogging)
        {
            _connection.Execute(WalPragma);
        }

        _queue = new(settings, _connection);
    }

    /// <summary>
    /// Executes command in batch and returns once executed.
    /// See <see cref="BatchOpsSettings"/> for settings.
    /// </summary>
    /// <param name="commandText">SQL text</param>
    /// <param name="param">SQL param</param>
    /// <returns>Task to await</returns>
    public Task<long?> ExecuteAsync(string commandText, object? param = null)
        => ExecuteAsync(new BatchCommand(commandText, param));

    /// <summary>
    /// Executes multiple commands in transaction
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <returns></returns>
    public Task<long?> ExecuteAsync(BatchCommand command)
    {
        _queue.Enqueue(command);
        return command.CompletionSource.Task;
    }

    public void Dispose()
    {
        _queue.Dispose();
        _connection.Dispose();
    }
}
