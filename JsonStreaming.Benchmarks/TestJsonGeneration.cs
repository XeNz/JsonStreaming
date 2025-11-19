using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace JsonStreaming.Benchmarks;

/// <summary>
///     Simple test class to verify JSON generation works correctly.
///     Run with: dotnet run -c Debug --project JsonStreaming.Benchmarks -- test
/// </summary>
public static class TestJsonGeneration
{
    public static async Task TestAsync()
    {
        Console.WriteLine("Testing JSON generation...\n");

        // Generate sample JSON
        var items = new List<string>();
        for (var i = 0; i < 3; i++)
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

        var json = $"[{string.Join(",", items)}]";

        Console.WriteLine("Generated JSON:");
        Console.WriteLine(json);
        Console.WriteLine();

        // Test deserialization
        try
        {
            var pipe = new Pipe();
            var bytes = Encoding.UTF8.GetBytes(json);
            var span = pipe.Writer.GetSpan(bytes.Length);
            bytes.CopyTo(span);
            pipe.Writer.Advance(bytes.Length);
            pipe.Writer.Complete();

            var typeInfo = BenchmarkJsonContext.Default.BenchmarkDto;
            var count = 0;

            Console.WriteLine("Testing streaming deserialization:");
            await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
            {
                count++;
                Console.WriteLine($"  Item {count}: Id={item.Id}, Name={item.Name}, Value={item.Value}, IsActive={item.IsActive}");
            }

            Console.WriteLine($"\nSuccessfully deserialized {count} items!");

            // Also test non-streaming
            var array = JsonSerializer.Deserialize(json, BenchmarkJsonContext.Default.BenchmarkDtoArray);
            Console.WriteLine($"Non-streaming deserialization: {array?.Length ?? 0} items");

            Console.WriteLine("\n✓ All tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
}
