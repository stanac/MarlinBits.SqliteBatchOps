using System.Diagnostics.CodeAnalysis;

namespace MarlinBits.SqliteBatchOps.BatchOps;

/// <summary>
/// Batch command, used when multiple commands needs to be executed in transaction
/// </summary>
public class BatchCommand
{
    public BatchCommand()
    {
    }

    [SetsRequiredMembers]
    public BatchCommand(string commandText, object? param = null, bool getChanges = false)
    {
        CommandText = commandText;
        Param = param;
        GetChanges = getChanges;
    }

    public required string CommandText { get; init; }
    public required object? Param { get; init; }
    public required bool GetChanges { get; init; }
    internal long? Result { get; set; }
    internal TaskCompletionSource<long?> CompletionSource { get; } = new();
    internal Exception? Error { get; set; }
    internal bool IsCompleted { get; private set; }

    internal void Complete()
    {
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;

        if (Error is not null)
        {
            CompletionSource.SetException(Error);
        }
        else
        {
            CompletionSource.SetResult(Result);
        }
    }
}

//public class BatchCommand<T>(string CommandText, object? Param = null) : BatchCommand
//{
//    public TaskCompletionSource<T?>? CompletionSource { get; set; }
//}