using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace JsonStreaming.Tests;

public class JsonStreamReaderTests
{
    [Fact]
    public async Task ReadArrayAsync_EmptyArray_ReturnsNoItems()
    {
        // Arrange
        var json = "[]";
        var pipe = CreatePipe(json);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<TestDto>(pipe.Reader))
        {
            results.Add(item);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ReadArrayAsync_SingleItem_ReturnsSingleItem()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var pipe = CreatePipe(json);
        var typeInfo = TestJsonContext.Default.TestDto;

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
        Assert.Equal(10.5m, results[0].Value);
    }

    [Fact]
    public async Task ReadArrayAsync_MultipleItems_ReturnsAllItems()
    {
        // Arrange
        var json = """
                   [
                       {"id": 1, "name": "First", "value": 10.5},
                       {"id": 2, "name": "Second", "value": 20.5},
                       {"id": 3, "name": "Third", "value": 30.5}
                   ]
                   """;
        var pipe = CreatePipe(json);
        var typeInfo = TestJsonContext.Default.TestDto;

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
        Assert.Equal("Third", results[2].Name);
    }

    [Fact]
    public async Task ReadArrayAsync_WithTypeInfo_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var pipe = CreatePipe(json);
        var typeInfo = TestJsonContext.Default.TestDto;

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public async Task ReadArrayAsync_WithRegistry_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var pipe = CreatePipe(json);
        var registry = new JsonTypeInfoRegistry();
        registry.Register(TestJsonContext.Default.TestDto);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<TestDto>(pipe.Reader, registry: registry))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public async Task ReadArrayAsync_CaseInsensitive_DeserializesCorrectly()
    {
        // Arrange
        var json = """[{"ID": 1, "NAME": "Test", "VALUE": 10.5}]""";
        var pipe = CreatePipe(json);
        var typeInfo = TestJsonContext.Default.TestDto;

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
    }

    [Fact]
    public async Task ReadArrayAsync_ChunkedData_HandlesCorrectly()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var pipe = new Pipe();
        var typeInfo = TestJsonContext.Default.TestDto;

        // Write data in small chunks
        var bytes = Encoding.UTF8.GetBytes(json);
        for (var i = 0; i < bytes.Length; i += 5)
        {
            var chunk = bytes.Skip(i).Take(5).ToArray();
            await pipe.Writer.WriteAsync(chunk);
            await pipe.Writer.FlushAsync();
        }

        pipe.Writer.Complete();

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public async Task ReadArrayAsync_WithCancellation_StopsReading()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var pipe = CreatePipe(json);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in JsonStreamReader.ReadArrayAsync<TestDto>(pipe.Reader, cancellationToken: cts.Token))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task ReadArrayAsync_PrimitiveTypes_HandlesCorrectly()
    {
        // Arrange
        var json = """[1, 2, 3, 4, 5]""";
        var pipe = CreatePipe(json);

        // Act
        var results = new List<int>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<int>(pipe.Reader))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(5, results.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, results);
    }

    [Fact]
    public async Task ReadArrayAsync_StringArray_HandlesCorrectly()
    {
        // Arrange
        var json = """["first", "second", "third"]""";
        var pipe = CreatePipe(json);

        // Act
        var results = new List<string>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<string>(pipe.Reader))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(new[] { "first", "second", "third" }, results);
    }

    [Fact]
    public async Task ReadArrayAsync_WithCustomReaderOptions_UsesOptions()
    {
        // Arrange - JSON with comments (which default options would skip)
        var json = """
                   [
                       // This is a comment
                       {"id": 1, "name": "Test", "value": 10.5}
                   ]
                   """;
        var pipe = CreatePipe(json);
        var typeInfo = TestJsonContext.Default.TestDto;
        var readerOptions = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true, MaxDepth = 64 };

        // Act
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo, readerOptions: readerOptions))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
    }

    [Fact]
    public async Task ReadArrayAsync_WithStrictReaderOptions_RejectsComments()
    {
        // Arrange - JSON with comments
        var json = """
                   [
                       // This comment should cause an error
                       {"id": 1, "name": "Test", "value": 10.5}
                   ]
                   """;
        var pipe = CreatePipe(json);
        var typeInfo = TestJsonContext.Default.TestDto;
        var readerOptions = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow };

        // Act & Assert
        await Assert.ThrowsAnyAsync<JsonException>(async () =>
        {
            await foreach (var item in JsonStreamReader.ReadArrayAsync(pipe.Reader, typeInfo: typeInfo, readerOptions: readerOptions))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task ReadArrayAsync_WithoutSourceGeneration_DeserializesCorrectly()
    {
        // Arrange - Use PascalCase JSON to match properties (no source generation means case-sensitive by default)
        var json = """[{"Id": 1, "Name": "Test", "Value": 10.5}]""";
        var pipe = CreatePipe(json);

        // Act - No typeInfo or registry provided
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<TestDto>(pipe.Reader))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
        Assert.Equal(10.5m, results[0].Value);
    }

    [Fact]
    public async Task ReadArrayAsync_WithoutSourceGeneration_MultipleItems()
    {
        // Arrange - Use PascalCase JSON
        var json = """
                   [
                       {"Id": 1, "Name": "First", "Value": 10.5},
                       {"Id": 2, "Name": "Second", "Value": 20.5},
                       {"Id": 3, "Name": "Third", "Value": 30.5}
                   ]
                   """;
        var pipe = CreatePipe(json);

        // Act - No typeInfo or registry
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<TestDto>(pipe.Reader))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
        Assert.Equal("Third", results[2].Name);
    }

    [Fact]
    public async Task ReadArrayAsync_WithoutSourceGeneration_WithOptions()
    {
        // Arrange - Use custom JsonSerializerOptions instead of TypeInfo
        var json = """[{"Id": 1, "Name": "Test", "Value": 10.5}]""";
        var pipe = CreatePipe(json);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act - No typeInfo, using options
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<TestDto>(pipe.Reader, options))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
    }

    [Fact]
    public async Task ReadArrayAsync_WithoutSourceGeneration_PrimitiveTypes()
    {
        // Arrange
        var json = """[100, 200, 300]""";
        var pipe = CreatePipe(json);

        // Act - No typeInfo for primitive type
        var results = new List<int>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<int>(pipe.Reader))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(new[] { 100, 200, 300 }, results);
    }

    [Fact]
    public async Task ReadArrayAsync_WithoutSourceGeneration_ComplexNesting()
    {
        // Arrange - Use PascalCase JSON
        var json = """
                   [
                       {"Id": 1, "Name": "Item1", "Value": 10.5},
                       {"Id": 2, "Name": "Item2", "Value": 20.5}
                   ]
                   """;
        var pipe = CreatePipe(json);

        // Act - No typeInfo
        var results = new List<TestDto>();
        await foreach (var item in JsonStreamReader.ReadArrayAsync<TestDto>(pipe.Reader))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results,
            item =>
            {
                Assert.True(item.Id > 0);
                Assert.NotNull(item.Name);
                Assert.True(item.Value > 0);
            });
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
}
