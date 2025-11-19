using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JsonStreaming;

public static class JsonStreamServiceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds JSON streaming support with manual JsonTypeInfo registration.
        ///     This method does not use reflection.
        /// </summary>
        /// <remarks>
        ///     Use this when you need a callback to dynamically register types or want flexible control over registration.
        ///     This is AOT compatible and does not use reflection.
        /// </remarks>
        /// <example>
        ///     <code>
        /// builder.Services.AddJsonStreamTypeInfo(registry =>
        /// {
        ///     registry.RegisterTypeInfo(MyJsonContext.Default.MyDto);
        ///     registry.RegisterTypeInfo(MyJsonContext.Default.OtherDto);
        /// });
        /// </code>
        /// </example>
        public IServiceCollection AddJsonStreamTypeInfo(Action<JsonTypeInfoRegistry> configure)
        {
            services.TryAddSingleton<JsonTypeInfoRegistry>();
            services.TryAddSingleton<IJsonTypeInfoRegistry>(sp => sp.GetRequiredService<JsonTypeInfoRegistry>());

            services.AddSingleton<IJsonTypeInfoRegistry>(sp =>
            {
                var registry = sp.GetRequiredService<JsonTypeInfoRegistry>();
                configure(registry);
                return registry;
            });

            return services;
        }

        /// <summary>
        ///     Adds JSON streaming support for a single type. Does not use reflection.
        /// </summary>
        /// <remarks>
        ///     Use this when you only need to register a single type. This is the most concise syntax.
        ///     This is AOT compatible and does not use reflection.
        /// </remarks>
        /// <example>
        ///     <code>
        /// builder.Services.AddJsonStreamType(MyJsonContext.Default.MyDto);
        /// </code>
        /// </example>
        public IServiceCollection AddJsonStreamType<T>(JsonTypeInfo<T> typeInfo)
        {
            services.TryAddSingleton<JsonTypeInfoRegistry>();

            services.AddSingleton<IJsonTypeInfoRegistry>(sp =>
            {
                var registry = sp.GetRequiredService<JsonTypeInfoRegistry>();
                registry.Register(typeInfo);
                return registry;
            });

            return services;
        }

        /// <summary>
        ///     Adds JSON streaming support with explicit type registration using a fluent builder pattern.
        ///     Does not use reflection.
        /// </summary>
        /// <remarks>
        ///     Use this when you need to register multiple types with a readable fluent API.
        ///     This is AOT compatible and does not use reflection.
        /// </remarks>
        /// <example>
        ///     <code>
        /// builder.Services.AddJsonStreamTypes()
        ///     .Add(MyJsonContext.Default.MyDto)
        ///     .Add(MyJsonContext.Default.OtherDto)
        ///     .Add(MyJsonContext.Default.ThirdDto)
        ///     .Build();
        /// </code>
        /// </example>
        public JsonStreamTypeBuilder AddJsonStreamTypes()
        {
            services.TryAddSingleton<JsonTypeInfoRegistry>();
            services.TryAddSingleton<IJsonTypeInfoRegistry>(sp => sp.GetRequiredService<JsonTypeInfoRegistry>());

            return new JsonStreamTypeBuilder(services);
        }

        /// <summary>
        ///     Adds JSON streaming support by automatically registering all JsonTypeInfo from a JsonSerializerContext.
        ///     This method uses reflection at startup.
        /// </summary>
        /// <remarks>
        ///     Use this for convenience when you want all types in a context automatically discovered.
        ///     Note: This method uses reflection and is NOT compatible with Native AOT.
        ///     Consider using AddJsonStreamTypeInfo, AddJsonStreamType, or AddJsonStreamTypes for AOT scenarios.
        /// </remarks>
        /// <example>
        ///     <code>
        /// builder.Services.AddJsonStreamContext&lt;MyJsonContext&gt;();
        /// </code>
        /// </example>
        public IServiceCollection AddJsonStreamContext<TContext>()
            where TContext : JsonSerializerContext
        {
            services.TryAddSingleton<JsonTypeInfoRegistry>();
            services.TryAddSingleton<IJsonTypeInfoRegistry>(sp =>
            {
                var registry = sp.GetRequiredService<JsonTypeInfoRegistry>();
                var context = Activator.CreateInstance<TContext>()!;

                foreach (var typeInfo in context.GetType().GetProperties())
                {
                    if (typeInfo.PropertyType.IsGenericType &&
                        typeInfo.PropertyType.GetGenericTypeDefinition() == typeof(JsonTypeInfo<>))
                    {
                        var value = typeInfo.GetValue(context);
                        if (value != null)
                        {
                            var targetType = typeInfo.PropertyType.GetGenericArguments()[0];
                            var registerMethod = typeof(JsonTypeInfoRegistry)
                                .GetMethod(nameof(JsonTypeInfoRegistry.Register))!
                                .MakeGenericMethod(targetType);
                            registerMethod.Invoke(registry, [value]);
                        }
                    }
                }

                return registry;
            });

            return services;
        }
    }

    extension(JsonTypeInfoRegistry registry)
    {
        /// <summary>
        ///     Fluent API for registering JsonTypeInfo in a registry.
        /// </summary>
        public JsonTypeInfoRegistry RegisterTypeInfo<T>(JsonTypeInfo<T> typeInfo)
        {
            registry.Register(typeInfo);
            return registry;
        }
    }
}
