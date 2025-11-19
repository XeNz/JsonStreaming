using System.Text.Json.Serialization;

namespace JsonStreaming.Tests;

public record TestDto(int Id, string Name, decimal Value);

public record SimpleDto(string Message);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TestDto))]
[JsonSerializable(typeof(SimpleDto))]
public partial class TestJsonContext : JsonSerializerContext
{
}
