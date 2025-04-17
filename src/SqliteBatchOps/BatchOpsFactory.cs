namespace SqliteBatchOps;

public class BatchOpsFactory : IDisposable
{
    private readonly Dictionary<string, BatchOps> _batchOps = new();
    private readonly Dictionary<string, BatchOpsSettings> _settings = new();
    private static readonly Lock _lock = new();

    /// <summary>
    /// Gets batch ops if settings are not set (<see cref="SetSettingsForBatchOps"/>)
    /// default settings are used.
    /// </summary>
    /// <param name="connectionString">Sqlite connection string</param>
    /// <returns>BatchOps that can be used to execute commands in batch</returns>
    public BatchOps GetBatchOps(string connectionString)
    {
        lock (_lock)
        {
            if (_batchOps.TryGetValue(connectionString, out BatchOps? value))
            {
                return value;
            }

            BatchOps batchOps = new BatchOps(connectionString, _settings.GetValueOrDefault(connectionString) ?? new BatchOpsSettings());
            _batchOps[connectionString] = batchOps;
            return batchOps;
        }
    }

    public void SetSettingsForBatchOps(string connectionString, BatchOpsSettings settings)
    {
        lock (_lock)
        {
            _settings[connectionString] = settings;
        }
    }

    public void Dispose()
    {
        foreach (BatchOps batchOps in _batchOps.Values)
        {
            batchOps.Dispose();
        }
    }
}