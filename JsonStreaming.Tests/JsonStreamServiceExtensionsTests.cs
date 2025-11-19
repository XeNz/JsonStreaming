using Microsoft.Extensions.DependencyInjection;

namespace JsonStreaming.Tests;

public class JsonStreamServiceExtensionsTests
{
    [Fact]
    public void AddJsonStreamTypeInfo_RegistersSingletonRegistry()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonStreamTypeInfo(registry =>
        {
            registry.RegisterTypeInfo(TestJsonContext.Default.TestDto);
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var registry1 = provider.GetService<IJsonTypeInfoRegistry>();
        var registry2 = provider.GetService<IJsonTypeInfoRegistry>();
        Assert.NotNull(registry1);
        Assert.Same(registry1, registry2); // Should be singleton
    }

    [Fact]
    public void AddJsonStreamTypeInfo_RegistersTypeInfo()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonStreamTypeInfo(registry =>
        {
            registry.RegisterTypeInfo(TestJsonContext.Default.TestDto);
        });
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IJsonTypeInfoRegistry>();

        // Assert
        var typeInfo = registry.GetTypeInfo<TestDto>();
        Assert.NotNull(typeInfo);
    }

    [Fact]
    public void AddJsonStreamTypeInfo_SupportsFluentAPI()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonStreamTypeInfo(registry =>
        {
            registry.RegisterTypeInfo(TestJsonContext.Default.TestDto)
                .RegisterTypeInfo(TestJsonContext.Default.SimpleDto);
        });
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IJsonTypeInfoRegistry>();

        // Assert
        var testDtoInfo = registry.GetTypeInfo<TestDto>();
        var simpleDtoInfo = registry.GetTypeInfo<SimpleDto>();
        Assert.NotNull(testDtoInfo);
        Assert.NotNull(simpleDtoInfo);
    }

    [Fact]
    public void AddJsonStreamType_RegistersSingleType()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonStreamType(TestJsonContext.Default.TestDto);
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IJsonTypeInfoRegistry>();

        // Assert
        var typeInfo = registry.GetTypeInfo<TestDto>();
        Assert.NotNull(typeInfo);
    }

    [Fact]
    public void AddJsonStreamTypes_BuilderPattern_RegistersMultipleTypes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonStreamTypes()
            .Add(TestJsonContext.Default.TestDto)
            .Add(TestJsonContext.Default.SimpleDto)
            .Build();
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IJsonTypeInfoRegistry>();

        // Assert
        var testDtoInfo = registry.GetTypeInfo<TestDto>();
        var simpleDtoInfo = registry.GetTypeInfo<SimpleDto>();
        Assert.NotNull(testDtoInfo);
        Assert.NotNull(simpleDtoInfo);
    }

    [Fact]
    public void AddJsonStreamContext_RegistersAllTypesFromContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonStreamContext<TestJsonContext>();
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IJsonTypeInfoRegistry>();

        // Assert
        var testDtoInfo = registry.GetTypeInfo<TestDto>();
        var simpleDtoInfo = registry.GetTypeInfo<SimpleDto>();
        Assert.NotNull(testDtoInfo);
        Assert.NotNull(simpleDtoInfo);
    }

    [Fact]
    public void AddJsonStreamTypeInfo_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonStreamTypeInfo(registry =>
        {
            registry.RegisterTypeInfo(TestJsonContext.Default.TestDto);
        });
        services.AddJsonStreamTypeInfo(registry =>
        {
            registry.RegisterTypeInfo(TestJsonContext.Default.SimpleDto);
        });

        // Assert
        var exception = Record.Exception(() => services.BuildServiceProvider());
        Assert.Null(exception);
    }

    [Fact]
    public void JsonStreamTypeBuilder_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddJsonStreamTypes()
            .Add(TestJsonContext.Default.TestDto)
            .Build();

        // Assert
        Assert.Same(services, result);
    }
}
