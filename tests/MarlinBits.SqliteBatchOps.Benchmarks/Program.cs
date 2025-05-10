using BenchmarkDotNet.Running;

namespace MarlinBits.SqliteBatchOps.Benchmarks;

internal class Program
{
    static void Main()
    {
        // BenchmarkRunner.Run(typeof(Program).Assembly);

        BenchmarkRunner.Run<BatchWithErrorsInsert>();
    }
}
