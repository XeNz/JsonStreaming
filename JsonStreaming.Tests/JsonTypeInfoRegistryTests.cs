namespace JsonStreaming.Tests;

public class JsonTypeInfoRegistryTests
{
    [Fact]
    public void Register_SingleType_CanBeRetrieved()
    {
        // Arrange
        var registry = new JsonTypeInfoRegistry();
        var typeInfo = TestJsonContext.Default.TestDto;

        // Act
        registry.Register(typeInfo);
        var retrieved = registry.GetTypeInfo<TestDto>();

        // Assert
        Assert.NotNull(retrieved);
        Assert.Same(typeInfo, retrieved);
    }

    [Fact]
    public void Register_MultipleTypes_AllCanBeRetrieved()
    {
        // Arrange
        var registry = new JsonTypeInfoRegistry();
        var testDtoTypeInfo = TestJsonContext.Default.TestDto;
        var simpleDtoTypeInfo = TestJsonContext.Default.SimpleDto;

        // Act
        registry.Register(testDtoTypeInfo);
        registry.Register(simpleDtoTypeInfo);

        var retrievedTestDto = registry.GetTypeInfo<TestDto>();
        var retrievedSimpleDto = registry.GetTypeInfo<SimpleDto>();

        // Assert
        Assert.NotNull(retrievedTestDto);
        Assert.NotNull(retrievedSimpleDto);
        Assert.Same(testDtoTypeInfo, retrievedTestDto);
        Assert.Same(simpleDtoTypeInfo, retrievedSimpleDto);
    }

    [Fact]
    public void GetTypeInfo_UnregisteredType_ReturnsNull()
    {
        // Arrange
        var registry = new JsonTypeInfoRegistry();

        // Act
        var retrieved = registry.GetTypeInfo<TestDto>();

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void Register_SameTypeTwice_OverwritesPrevious()
    {
        // Arrange
        var registry = new JsonTypeInfoRegistry();
        var typeInfo1 = TestJsonContext.Default.TestDto;
        var typeInfo2 = TestJsonContext.Default.TestDto; // Same reference in this case

        // Act
        registry.Register(typeInfo1);
        registry.Register(typeInfo2);
        var retrieved = registry.GetTypeInfo<TestDto>();

        // Assert
        Assert.NotNull(retrieved);
        Assert.Same(typeInfo2, retrieved);
    }

    [Fact]
    public void IJsonTypeInfoRegistry_Interface_WorksCorrectly()
    {
        // Arrange
        IJsonTypeInfoRegistry registry = new JsonTypeInfoRegistry();
        var typeInfo = TestJsonContext.Default.TestDto;

        // Act
        ((JsonTypeInfoRegistry)registry).Register(typeInfo);
        var retrieved = registry.GetTypeInfo<TestDto>();

        // Assert
        Assert.NotNull(retrieved);
        Assert.Same(typeInfo, retrieved);
    }
}
