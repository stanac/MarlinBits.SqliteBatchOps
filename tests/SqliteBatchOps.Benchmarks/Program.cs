using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace SqliteBatchOps.Benchmarks;

internal class Program
{
    static void Main()
    {
        BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
