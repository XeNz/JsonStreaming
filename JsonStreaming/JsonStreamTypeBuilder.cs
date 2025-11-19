using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace JsonStreaming;

/// <summary>
///     Builder for registering multiple JsonTypeInfo without reflection.
/// </summary>
/// <remarks>
///     This builder provides a fluent API for registering multiple types without any reflection.
///     It is fully AOT compatible.
/// </remarks>
public sealed class JsonStreamTypeBuilder
{
    private readonly List<Action<JsonTypeInfoRegistry>> _registrations = new();
    private readonly IServiceCollection _services;

    public JsonStreamTypeBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    ///     Add a type to the registry.
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    /// <param name="typeInfo">The source-generated JsonTypeInfo for the type</param>
    /// <returns>The builder for chaining</returns>
    public JsonStreamTypeBuilder Add<T>(JsonTypeInfo<T> typeInfo)
    {
        _registrations.Add(registry => registry.Register(typeInfo));
        return this;
    }

    /// <summary>
    ///     Finalize the registration and return the service collection.
    /// </summary>
    /// <returns>The IServiceCollection for further configuration</returns>
    public IServiceCollection Build()
    {
        _services.AddSingleton<IJsonTypeInfoRegistry>(sp =>
        {
            var registry = sp.GetRequiredService<JsonTypeInfoRegistry>();
            foreach (var registration in _registrations)
            {
                registration(registry);
            }

            return registry;
        });

        return _services;
    }
}
