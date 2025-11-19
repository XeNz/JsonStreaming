using BenchmarkDotNet.Running;
using JsonStreaming.Benchmarks;

// Check for test mode
if (args.Length > 0 && args[0] == "test")
{
    await TestJsonGeneration.TestAsync();
    return;
}

// Run all benchmarks
var summaries = new[]
{
    BenchmarkRunner.Run<StreamingVsNonStreamingBenchmark>(), BenchmarkRunner.Run<DeserializationMethodBenchmark>(), BenchmarkRunner.Run<MemoryAndThroughputBenchmark>()
};

Console.WriteLine("\n=== Benchmark Summary ===");
Console.WriteLine($"Total benchmarks run: {summaries.Length}");
Console.WriteLine("\nTo run a specific benchmark, use:");
Console.WriteLine("  dotnet run -c Release --filter *StreamingVsNonStreamingBenchmark*");
Console.WriteLine("  dotnet run -c Release --filter *DeserializationMethodBenchmark*");
Console.WriteLine("  dotnet run -c Release --filter *MemoryAndThroughputBenchmark*");
Console.WriteLine("\nTo test JSON generation:");
Console.WriteLine("  dotnet run -c Debug --project JsonStreaming.Benchmarks -- test");
