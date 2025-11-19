using System.Text.Json.Serialization;

namespace JsonStreaming.Example;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(MyDto))]
public partial class MyJsonContext : JsonSerializerContext
{
}
