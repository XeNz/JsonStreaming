using System.Text.Json.Serialization.Metadata;

namespace JsonStreaming;

/// <summary>
///     Provides access to registered JsonTypeInfo instances by type.
/// </summary>
public interface IJsonTypeInfoRegistry
{
    /// <summary>
    ///     Retrieves the JsonTypeInfo for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get JsonTypeInfo for</typeparam>
    /// <returns>The JsonTypeInfo if registered, otherwise null</returns>
    JsonTypeInfo<T>? GetTypeInfo<T>();
}

/// <inheritdoc />
/// <summary>
///     Registry for storing and retrieving source-generated JsonTypeInfo instances.
/// </summary>
/// <remarks>
///     This registry enables AOT-compatible JSON serialization by storing JsonTypeInfo instances
///     from source-generated contexts. Use this with AddJsonStreamTypeInfo, AddJsonStreamType,
///     or AddJsonStreamTypes to register types in dependency injection.
/// </remarks>
public sealed class JsonTypeInfoRegistry : IJsonTypeInfoRegistry
{
    private readonly Dictionary<Type, object> _typeInfos = new();

    /// <inheritdoc />
    /// <summary>
    ///     Retrieves the JsonTypeInfo for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get JsonTypeInfo for</typeparam>
    /// <returns>The JsonTypeInfo if registered, otherwise null</returns>
    public JsonTypeInfo<T>? GetTypeInfo<T>()
    {
        return _typeInfos.TryGetValue(typeof(T), out var typeInfo)
            ? (JsonTypeInfo<T>)typeInfo
            : null;
    }

    /// <summary>
    ///     Registers a JsonTypeInfo for a specific type.
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    /// <param name="typeInfo">The source-generated JsonTypeInfo for the type</param>
    /// <remarks>
    ///     If a type is registered multiple times, the last registration wins.
    /// </remarks>
    public void Register<T>(JsonTypeInfo<T> typeInfo)
    {
        _typeInfos[typeof(T)] = typeInfo;
    }
}
