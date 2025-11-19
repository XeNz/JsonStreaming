using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace JsonStreaming;

/// <summary>
///     Provides automatic parameter binding for streaming JSON arrays in ASP.NET Core minimal APIs.
/// </summary>
/// <typeparam name="T">The type of objects to deserialize from the JSON array</typeparam>
/// <remarks>
///     This binder enables streaming JSON array deserialization directly in minimal API endpoints.
///     It automatically binds to the request body and uses registered JsonTypeInfo from dependency injection
///     for AOT-compatible deserialization. The binder implements IAsyncEnumerable to allow direct enumeration
///     in endpoint handlers.
/// </remarks>
/// <example>
///     <code>
/// app.MapPost("/process", async (JsonStream&lt;MyDto&gt; items) =&gt;
/// {
///     await foreach (var item in items)
///     {
///         // Process each item as it arrives
///     }
///     return Results.Ok();
/// });
/// </code>
/// </example>
public sealed class JsonStream<T> : IAsyncEnumerable<T>, IEndpointParameterMetadataProvider
{
    private readonly IAsyncEnumerable<T> _stream;


    private JsonStream(IAsyncEnumerable<T> stream)
    {
        _stream = stream;
    }

    /// <inheritdoc />
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        await foreach (var item in _stream.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new FromBodyAttribute());
    }

    /// <summary>
    ///     Binds the JsonStream to the HTTP request body.
    /// </summary>
    /// <param name="context">The HttpContext for the current request</param>
    /// <param name="parameter">The parameter information from the endpoint method (unused but required by binding contract)</param>
    /// <returns>A JsonStream instance that streams the deserialized objects</returns>
    /// <remarks>
    ///     This method is called automatically by ASP.NET Core's parameter binding infrastructure.
    ///     It retrieves JsonSerializerOptions, IJsonTypeInfoRegistry, and JsonReaderOptions from dependency injection
    ///     and creates a streaming reader for the request body. If no JsonSerializerOptions are registered,
    ///     it defaults to JsonSerializerDefaults.Web (camelCase property names, case-insensitive).
    /// </remarks>
    public static ValueTask<JsonStream<T>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var pipeReader = context.Request.BodyReader;
        var options = context.RequestServices.GetService<JsonSerializerOptions>() ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var registry = context.RequestServices.GetService<IJsonTypeInfoRegistry>();
        var readerOptions = context.RequestServices.GetService<JsonReaderOptions?>();
        var stream = JsonStreamReader.ReadArrayAsync<T>(pipeReader, options, registry: registry, readerOptions: readerOptions);

        return new ValueTask<JsonStream<T>>(new JsonStream<T>(stream));
    }
}
