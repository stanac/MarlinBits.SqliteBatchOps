namespace Marlin.BatchOps;

/// <summary>
/// Settings for <see cref="BatchOps"/>
/// </summary>
public class BatchOpsSettings
{
    private int _millisecondsWait = 50;
    private int _maxBatchSize = 5000;

    /// <summary>
    /// How long to wait before triggering batch in milliseconds, default is 500.
    /// Must be at least 1.
    /// </summary>
    public int MillisecondsWait
    {
        get => _millisecondsWait;
        set
        {
            if (value < 1)
            {
                throw new ArgumentException("Value must be at least 1");
            }

            _millisecondsWait = value;
        }
    }

    /// <summary>
    /// Maximum batch size before execution, must be at least 2.
    /// Default value is 1000.
    /// </summary>
    public int MaxBatchSize
    {
        get => _maxBatchSize;
        set
        {
            if (value < 2)
            {
                throw new ArgumentException("Value must be at least 2.");
            }
            _maxBatchSize = value;
        }
    }

    /// <summary>
    /// Use WAL
    /// </summary>
    public bool UseWriteAheadLogging { get; set; }
}