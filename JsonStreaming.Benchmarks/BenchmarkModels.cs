using System.Text.Json.Serialization;

namespace JsonStreaming.Benchmarks;

public record BenchmarkDto(int Id, string Name, decimal Value, DateTime Timestamp, bool IsActive);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(BenchmarkDto))]
[JsonSerializable(typeof(BenchmarkDto[]))]
public partial class BenchmarkJsonContext : JsonSerializerContext
{
}
