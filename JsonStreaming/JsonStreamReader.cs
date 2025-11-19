using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JsonStreaming;

/// <summary>
///     Provides methods for streaming JSON array deserialization using System.IO.Pipelines.
/// </summary>
public static class JsonStreamReader
{
    /// <summary>
    ///     Asynchronously reads and deserializes a JSON array from a PipeReader, yielding items as they are parsed.
    /// </summary>
    /// <typeparam name="T">The type of objects to deserialize</typeparam>
    /// <param name="reader">The PipeReader containing the JSON data</param>
    /// <param name="options">Optional JsonSerializerOptions for deserialization</param>
    /// <param name="typeInfo">Optional source-generated JsonTypeInfo for AOT-compatible deserialization</param>
    /// <param name="registry">Optional registry to retrieve JsonTypeInfo from dependency injection</param>
    /// <param name="readerOptions">Optional JsonReaderOptions to configure JSON reading behavior. If not specified, uses CommentHandling.Skip and AllowTrailingCommas = true</param>
    /// <param name="initialBufferSize">Initial buffer size (defaults to 32). Adjust based on expected items per chunk.</param>
    /// <param name="cancellationToken">Cancellation token to stop the enumeration</param>
    /// <returns>An async enumerable that yields deserialized objects as they are parsed</returns>
    /// <remarks>
    ///     This method streams JSON data using System.IO.Pipelines for efficient memory usage.
    ///     Items are yielded per pipeline buffer chunk to balance streaming performance with deserialization overhead.
    ///     The method supports both source-generated JsonTypeInfo (for AOT scenarios) and reflection-based deserialization.
    ///     Uses ArrayPool for buffer management to minimize allocations and GC pressure.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var pipe = new Pipe();
    /// await foreach (var item in JsonStreamReader.ReadArrayAsync&lt;MyDto&gt;(pipe.Reader))
    /// {
    ///     // Process each item as it arrives
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<T> ReadArrayAsync<T>(
        PipeReader reader,
        JsonSerializerOptions? options = null,
        JsonTypeInfo<T>? typeInfo = null,
        IJsonTypeInfoRegistry? registry = null,
        JsonReaderOptions? readerOptions = null,
        int initialBufferSize = 32,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveReaderOptions = readerOptions ?? new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        var jsonReaderState = new JsonReaderState(effectiveReaderOptions);
        var insideArray = false;
        var effectiveTypeInfo = typeInfo ?? registry?.GetTypeInfo<T>();

        // Rent initial buffer from ArrayPool
        var buffer = ArrayPool<T>.Shared.Rent(initialBufferSize);
        var count = 0;

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var readBuffer = result.Buffer;
                if (readBuffer.IsEmpty && result.IsCompleted)
                {
                    yield break;
                }

                var consumed = readBuffer.Start;
                var examined = readBuffer.End;
                var endOfArray = false;

                count = 0;

                {
                    var readerUtf8 = new Utf8JsonReader(readBuffer, result.IsCompleted, jsonReaderState);

                    while (readerUtf8.Read())
                    {
                        if (!insideArray)
                        {
                            if (readerUtf8.TokenType == JsonTokenType.StartArray)
                            {
                                insideArray = true;
                            }

                            continue;
                        }

                        if (readerUtf8.TokenType == JsonTokenType.EndArray)
                        {
                            insideArray = false;
                            consumed = readerUtf8.Position;
                            endOfArray = true;
                            break;
                        }

                        if (readerUtf8.TokenType is JsonTokenType.StartObject or
                            JsonTokenType.StartArray or
                            JsonTokenType.String or
                            JsonTokenType.Number or
                            JsonTokenType.True or
                            JsonTokenType.False or
                            JsonTokenType.Null)
                        {
                            // Resize buffer if needed
                            if (count >= buffer.Length)
                            {
                                var newBuffer = ArrayPool<T>.Shared.Rent(buffer.Length * 2);
                                Array.Copy(buffer, newBuffer, count);
                                ArrayPool<T>.Shared.Return(buffer);
                                buffer = newBuffer;
                            }

                            T value;
                            if (effectiveTypeInfo is not null)
                            {
                                value = JsonSerializer.Deserialize(ref readerUtf8, effectiveTypeInfo)!;
                            }
                            else
                            {
                                var element = JsonElement.ParseValue(ref readerUtf8);
                                value = element.Deserialize<T>(options)!;
                            }

                            consumed = readerUtf8.Position;
                            buffer[count++] = value;
                        }
                    }

                    jsonReaderState = readerUtf8.CurrentState;
                }

                reader.AdvanceTo(consumed, examined);

                // Yield items from rented buffer
                for (var i = 0; i < count; i++)
                {
                    yield return buffer[i];
                }

                if (endOfArray || result.IsCompleted)
                {
                    break;
                }
            }
        }
        finally
        {
            // Always return buffer to pool
            ArrayPool<T>.Shared.Return(buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }
}
