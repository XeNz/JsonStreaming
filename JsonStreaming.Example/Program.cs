using System.Text.Json.Nodes;
using JsonStreaming;
using JsonStreaming.Example;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register source-generated JsonTypeInfo
// Choose ONE of the following options:

// Option 1: Automatic registration (uses reflection at startup)
// builder.Services.AddJsonStreamContext<MyJsonContext>();

// Option 2: Manual registration with callback (NO reflection)
builder.Services.AddJsonStreamTypeInfo(registry =>
{
    registry.RegisterTypeInfo(MyJsonContext.Default.MyDto);
});

// Option 3: Single type registration (NO reflection)
// builder.Services.AddJsonStreamType(MyJsonContext.Default.MyDto);

// Option 4: Builder pattern for multiple types (NO reflection, most explicit)
// builder.Services.AddJsonStreamTypes()
//     .Add(MyJsonContext.Default.MyDto)
//     .Add(MyJsonContext.Default.OtherDto)
//     .Build();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

static async IAsyncEnumerable<MyDto> ProcessItems(IAsyncEnumerable<MyDto> items)
{
    await foreach (var item in items)
    {
        var transformed = item with { Id = item.Id + 1 };
        yield return transformed;
    }
}

app.MapPost("/process",
        (JsonStream<MyDto> items, ILogger<Program> logger) =>
        {
            var processed = ProcessItems(items)
                .Peek(item =>
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Processing item: {ItemName} at {Timestamp}", item.Name, DateTimeOffset.UtcNow);
                    }
                });
            return TypedResults.ServerSentEvents(
                processed
            );
        })
    .WithSummary("Process a stream of MyDto items")
    .WithDescription("Accepts an array of MyDto objects and processes them as a stream, streaming back processed results via Server-Sent Events")
    .Accepts<MyDto[]>("application/json")
    .Produces(200, contentType: "text/event-stream")
    .AddOpenApiOperationTransformer((operation, _, _) =>
    {
        const string exampleJson = """
                                   [
                                     {
                                       "id": 1,
                                       "name": "First Item"
                                     },
                                     {
                                       "id": 2,
                                       "name": "Second Item"
                                     },
                                     {
                                       "id": 3,
                                       "name": "Third Item"
                                     }
                                   ]
                                   """;
        var example = JsonNode.Parse(exampleJson);
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) == true)
        {
            mediaType.Example = example;
        }

        return Task.CompletedTask;
    });

app.Run();
