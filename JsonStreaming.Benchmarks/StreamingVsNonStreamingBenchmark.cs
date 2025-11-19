using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace JsonStreaming.Benchmarks;

/// <summary>
///     Compares streaming deserialization vs traditional non-streaming deserialization.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class StreamingVsNonStreamingBenchmark
{
    private string _jsonLarge = null!;
    private string _jsonMedium = null!;
    private string _jsonSmall = null!;

    [GlobalSetup]
    public void Setup()
    {
        _jsonSmall = GenerateJson(10);
        _jsonMedium = GenerateJson(100);
        _jsonLarge = GenerateJson(1000);
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

    private static Pipe CreatePipe(string json)
    {
        var pipe = new Pipe();
        var bytes = Encoding.UTF8.GetBytes(json);
        var span = pipe.Writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        pipe.Writer.Advance(bytes.Length);
        pipe.Writer.Complete();
        return pipe;
    }

    // ========== Small Dataset (10 items) ==========

    [Benchmark]
    public async Task<int> StreamingSmall()
    {
        var pipe = CreatePipe(_jsonSmall);
        var typeInfo = BenchmarkJsonContext.Default.BenchmarkDto;
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            count++;
        }

        return count;
    }

    [Benchmark(Baseline = true)]
    public int NonStreamingSmall()
    {
        var items = JsonSerializer.Deserialize(_jsonSmall, BenchmarkJsonContext.Default.BenchmarkDtoArray);
        return items?.Length ?? 0;
    }

    // ========== Medium Dataset (100 items) ==========

    [Benchmark]
    public async Task<int> StreamingMedium()
    {
        var pipe = CreatePipe(_jsonMedium);
        var typeInfo = BenchmarkJsonContext.Default.BenchmarkDto;
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int NonStreamingMedium()
    {
        var items = JsonSerializer.Deserialize(_jsonMedium, BenchmarkJsonContext.Default.BenchmarkDtoArray);
        return items?.Length ?? 0;
    }

    // ========== Large Dataset (1000 items) ==========

    [Benchmark]
    public async Task<int> StreamingLarge()
    {
        var pipe = CreatePipe(_jsonLarge);
        var typeInfo = BenchmarkJsonContext.Default.BenchmarkDto;
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int NonStreamingLarge()
    {
        var items = JsonSerializer.Deserialize(_jsonLarge, BenchmarkJsonContext.Default.BenchmarkDtoArray);
        return items?.Length ?? 0;
    }
}
