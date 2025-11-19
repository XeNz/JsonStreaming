using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace JsonStreaming.Benchmarks;

/// <summary>
///     Benchmarks focused on memory usage and throughput with different dataset sizes.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MemoryAndThroughputBenchmark
{
    private string _json10 = null!;
    private string _json100 = null!;
    private string _json1000 = null!;
    private string _json500 = null!;
    private string _json5000 = null!;

    [Params(10, 100, 500, 1000, 5000)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _json10 = GenerateJson(10);
        _json100 = GenerateJson(100);
        _json500 = GenerateJson(500);
        _json1000 = GenerateJson(1000);
        _json5000 = GenerateJson(5000);
    }

    private static string GenerateJson(int itemCount)
    {
        var items = new List<string>();
        for (var i = 0; i < itemCount; i++)
        {
            var isActive = i % 2 == 0 ? "true" : "false";
            var value = (i * 10.5m).ToString(CultureInfo.InvariantCulture);
            items.Add($@"{{
                ""id"": {i},
                ""name"": ""Item {i}"",
                ""value"": {value},
                ""timestamp"": ""2025-01-01T00:00:00Z"",
                ""isActive"": {isActive}
            }}");
        }

        return $"[{string.Join(",", items)}]";
    }

    private Pipe CreatePipe(string json)
    {
        var pipe = new Pipe();
        var bytes = Encoding.UTF8.GetBytes(json);
        var span = pipe.Writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        pipe.Writer.Advance(bytes.Length);
        pipe.Writer.Complete();
        return pipe;
    }

    [Benchmark]
    public async Task<int> StreamingDeserialization()
    {
        var json = ItemCount switch
        {
            10 => _json10,
            100 => _json100,
            500 => _json500,
            1000 => _json1000,
            5000 => _json5000,
            _ => throw new InvalidOperationException()
        };

        var pipe = CreatePipe(json);
        var typeInfo = BenchmarkJsonContext.Default.BenchmarkDto;
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            count++;
            // Simulate minimal processing
            _ = item.Id;
        }

        return count;
    }

    [Benchmark]
    public async Task<long> StreamingWithProcessing()
    {
        var json = ItemCount switch
        {
            10 => _json10,
            100 => _json100,
            500 => _json500,
            1000 => _json1000,
            5000 => _json5000,
            _ => throw new InvalidOperationException()
        };

        var pipe = CreatePipe(json);
        var typeInfo = BenchmarkJsonContext.Default.BenchmarkDto;
        long sum = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            // Simulate realistic processing
            sum += item.Id;
            if (item.IsActive)
            {
                sum += (long)item.Value;
            }
        }

        return sum;
    }
}
