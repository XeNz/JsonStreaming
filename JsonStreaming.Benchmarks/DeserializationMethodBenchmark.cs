using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace JsonStreaming.Benchmarks;

/// <summary>
///     Compares different deserialization methods: TypeInfo (source-generated), Registry, and JsonSerializerOptions.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class DeserializationMethodBenchmark
{
    private string _json = null!;
    private JsonSerializerOptions _options = null!;
    private JsonTypeInfoRegistry _registry = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate 100 items
        var items = new List<string>();
        for (var i = 0; i < 100; i++)
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

        _json = $"[{string.Join(",", items)}]";

        // Setup options
        _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Setup registry
        _registry = new JsonTypeInfoRegistry();
        _registry.Register(BenchmarkJsonContext.Default.BenchmarkDto);
    }

    private Pipe CreatePipe()
    {
        var pipe = new Pipe();
        var bytes = Encoding.UTF8.GetBytes(_json);
        var span = pipe.Writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        pipe.Writer.Advance(bytes.Length);
        pipe.Writer.Complete();
        return pipe;
    }

    [Benchmark(Baseline = true)]
    public async Task<int> WithTypeInfo()
    {
        var pipe = CreatePipe();
        var typeInfo = BenchmarkJsonContext.Default.BenchmarkDto;
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public async Task<int> WithRegistry()
    {
        var pipe = CreatePipe();
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync<BenchmarkDto>(pipe.Reader, registry: _registry))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public async Task<int> WithJsonSerializerOptions()
    {
        var pipe = CreatePipe();
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync<BenchmarkDto>(pipe.Reader, _options))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public async Task<int> WithoutAnyOptions()
    {
        var pipe = CreatePipe();
        var count = 0;

        await foreach (var item in JsonStreamReader.ReadArrayAsync<BenchmarkDto>(pipe.Reader))
        {
            count++;
        }

        return count;
    }
}
