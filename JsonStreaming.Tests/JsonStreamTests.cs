using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace JsonStreaming.Tests;

public class JsonStreamTests
{
    [Fact]
    public async Task BindAsync_CreatesBinderWithStream()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var context = CreateHttpContext(json);

        // Act
        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Assert
        Assert.NotNull(binder);
    }

    [Fact]
    public async Task GetAsyncEnumerator_EnumeratesItems()
    {
        // Arrange
        var json = """
                   [
                       {"id": 1, "name": "First", "value": 10.5},
                       {"id": 2, "name": "Second", "value": 20.5}
                   ]
                   """;
        var context = CreateHttpContext(json);
        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
    }

    [Fact]
    public async Task GetAsyncEnumerator_WithCancellation_StopsEnumeration()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var context = CreateHttpContext(json);
        var binder = await JsonStream<TestDto>.BindAsync(context, null!);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in binder.WithCancellation(cts.Token))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task BindAsync_WithRegisteredTypeInfo_UsesTypeInfo()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var registry = new JsonTypeInfoRegistry();
        registry.Register(TestJsonContext.Default.TestDto);

        var context = CreateHttpContext(json);
        var services = new ServiceCollection();
        services.AddSingleton<IJsonTypeInfoRegistry>(registry);
        context.RequestServices = services.BuildServiceProvider();

        // Act
        var binder = await JsonStream<TestDto>.BindAsync(context, null!);
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public async Task BindAsync_EmptyArray_ReturnsNoItems()
    {
        // Arrange
        var json = "[]";
        var context = CreateHttpContext(json);
        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task BindAsync_WithoutSourceGeneration_DeserializesCorrectly()
    {
        // Arrange - No registry in services, use PascalCase JSON
        var json = """[{"Id": 1, "Name": "Test", "Value": 10.5}]""";
        var context = CreateHttpContextWithoutRegistry(json);
        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
    }

    [Fact]
    public async Task BindAsync_WithoutSourceGeneration_MultipleItems()
    {
        // Arrange - Use PascalCase JSON
        var json = """
                   [
                       {"Id": 1, "Name": "First", "Value": 10.5},
                       {"Id": 2, "Name": "Second", "Value": 20.5}
                   ]
                   """;
        var context = CreateHttpContextWithoutRegistry(json);
        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
    }

    [Fact]
    public async Task BindAsync_WithoutSourceGeneration_WithJsonSerializerOptions()
    {
        // Arrange - Register JsonSerializerOptions without TypeInfo registry
        var json = """[{"Id": 1, "Name": "Test", "Value": 10.5}]""";
        var context = CreateHttpContextWithoutRegistry(json);

        var services = new ServiceCollection();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        services.AddSingleton(options);
        context.RequestServices = services.BuildServiceProvider();

        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
    }

    [Fact]
    public async Task BindAsync_WithoutOptions_DefaultsToWebDefaults()
    {
        // Arrange - camelCase JSON (Web defaults should handle this)
        var json = """[{"id": 1, "name": "Test", "value": 10.5}]""";
        var context = CreateHttpContextWithoutRegistry(json);

        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert - Should work because JsonSerializerDefaults.Web is case-insensitive and uses camelCase
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Test", results[0].Name);
        Assert.Equal(10.5m, results[0].Value);
    }

    [Fact]
    public async Task BindAsync_WithoutOptions_HandlesCamelCaseJson()
    {
        // Arrange - Multiple items with camelCase
        var json = """
                   [
                       {"id": 1, "name": "First", "value": 10.5},
                       {"id": 2, "name": "Second", "value": 20.5}
                   ]
                   """;
        var context = CreateHttpContextWithoutRegistry(json);

        var binder = await JsonStream<TestDto>.BindAsync(context, null!);

        // Act
        var results = new List<TestDto>();
        await foreach (var item in binder)
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
    }

    private static DefaultHttpContext CreateHttpContextWithoutRegistry(string json)
    {
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);
        context.Request.Body = stream;
        context.Request.ContentType = "application/json";

        // Create empty service provider (no registry)
        var services = new ServiceCollection();
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    private static DefaultHttpContext CreateHttpContext(string json)
    {
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);
        context.Request.Body = stream;
        context.Request.ContentType = "application/json";

        // Add a default service provider with registry
        var services = new ServiceCollection();
        var registry = new JsonTypeInfoRegistry();
        registry.Register(TestJsonContext.Default.TestDto);
        services.AddSingleton<IJsonTypeInfoRegistry>(registry);
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }
}
