using Dapper;
using Microsoft.Data.Sqlite;

namespace Marlin.BatchOps;

internal class CommandQueue : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Lock _lock = new();
    private readonly System.Timers.Timer _timer;
    private readonly List<BatchCommand> _commands = [];
    private readonly int _maxBatchSize;
    private bool _isDisposing;

    public CommandQueue(BatchOpsSettings settings, SqliteConnection connection)
    {
        _connection = connection;
        _timer = new();
        _timer.Interval = settings.MillisecondsWait;
        _maxBatchSize = settings.MaxBatchSize;

        _timer.Elapsed += (_, _) => ProcessAll();
        _timer.Start();
    }

    public void Enqueue(params BatchCommand[] commands)
    {
        bool enqueue = false;
        bool process = false;

        if (_isDisposing)
        {
            throw new InvalidOperationException("CommandQueue is disposed or begun disposing.");
        }

        lock (_lock)
        {
            if (_commands.Count + commands.Length > _maxBatchSize)
            {
                process = true;
                enqueue = true;
            }
            else
            {
                _commands.AddRange(commands);

                if (_commands.Count > _maxBatchSize)
                {
                    process = true;
                }
            }
        }

        if (process)
        {
            ProcessAll();
        }

        if (enqueue)
        {
            lock (_lock)
            {
                _commands.AddRange(commands);
            }
        }
    }

    public void ProcessAll()
    {
        lock (_lock)
        {
            if (_commands.Count == 0)
            {
                return;
            }

            ProcessAll(0);
            _commands.RemoveAll(x => x.IsCompleted);
        }
    }

    private void ProcessAll(int attempt)
    {
        if (attempt > 100)
        {
            return;
        }

        if (_commands.Count == 0)
        {
            return;
        }

        SqliteTransaction transaction = _connection.BeginTransaction();

        foreach (BatchCommand cmd in _commands)
        {
            try
            {
                Execute(cmd, transaction);
            }
            catch (Exception e)
            {
                cmd.Error = e;
                cmd.Complete();
                transaction.Rollback();
                transaction.Dispose();
                ProcessAll(attempt + 1);
                return;
            }
        }
        
        transaction.Commit();

        foreach (BatchCommand cmd in _commands)
        {
            cmd.Complete();
        }

        transaction.Dispose();
    }

    private void Execute(BatchCommand command, SqliteTransaction transaction)
    {
        if (command.IsCompleted)
        {
            return;
        }

        _connection.Execute(command.CommandText, command.Param, transaction);

        long? changes = null;

        if (command.GetChanges)
        {
            changes = _connection.QuerySingle<long>("SELECT CHANGES()");
        }

        command.Result = changes;
    }

    public void Dispose()
    {
        if (_isDisposing)
        {
            return;
        }

        _isDisposing = true;
        _timer.Stop();
        ProcessAll();
        _connection.Dispose();
        _timer.Dispose();
    }
}