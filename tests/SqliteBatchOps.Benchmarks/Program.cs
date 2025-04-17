using BenchmarkDotNet.Running;

namespace SqliteBatchOps.Benchmarks
{
    internal class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<InsertBenchmark>();
        }
    }
}
