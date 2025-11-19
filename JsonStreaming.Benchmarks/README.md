# JsonStreaming Benchmarks

This project contains performance benchmarks for the JsonStreaming library using BenchmarkDotNet.

## Running Benchmarks

### Run All Benchmarks

```bash
dotnet run -c Release --project JsonStreaming.Benchmarks
```

### Run Specific Benchmark

```bash
dotnet run -c Release --project JsonStreaming.Benchmarks --filter *StreamingVsNonStreamingBenchmark*
dotnet run -c Release --project JsonStreaming.Benchmarks --filter *DeserializationMethodBenchmark*
dotnet run -c Release --project JsonStreaming.Benchmarks --filter *MemoryAndThroughputBenchmark*
```

## Benchmark Descriptions

### StreamingVsNonStreamingBenchmark

Compares streaming JSON deserialization using `JsonStreamReader` vs traditional non-streaming deserialization with `JsonSerializer.Deserialize`.

**Datasets:**

- Small: 10 items
- Medium: 100 items
- Large: 1000 items

**What it measures:**

- Execution time for streaming vs non-streaming
- Memory allocations
- Throughput differences across dataset sizes

**Expected results:**

- Streaming should show better memory efficiency for larger datasets
- Non-streaming may be faster for very small datasets due to lower overhead
- Streaming enables processing items as they arrive without loading entire array

### DeserializationMethodBenchmark

Compares different deserialization configuration methods with 100 items.

**Methods compared:**

- **WithTypeInfo** (Baseline): Using source-generated `JsonTypeInfo<T>` directly
- **WithRegistry**: Using `JsonTypeInfoRegistry` for DI-based type resolution
- **WithJsonSerializerOptions**: Using reflection-based `JsonSerializerOptions`
- **WithoutAnyOptions**: Default deserialization without any configuration

**What it measures:**

- Performance overhead of registry lookup vs direct TypeInfo
- AOT-compatible (TypeInfo, Registry) vs reflection-based performance
- Memory allocations for each approach

**Expected results:**

- Direct TypeInfo should be fastest (baseline)
- Registry should have minimal overhead (single dictionary lookup)
- JsonSerializerOptions should be slower due to reflection
- No options should be slowest (uses reflection + no caching)

### MemoryAndThroughputBenchmark

Focused benchmark on memory usage and throughput across different dataset sizes.

**Dataset sizes:** 10, 100, 500, 1000, 5000 items (parameterized)

**Scenarios:**

- **StreamingDeserialization**: Pure deserialization with minimal processing
- **StreamingWithProcessing**: Realistic scenario with item processing (summing values)

**What it measures:**

- Memory allocation patterns at different scales
- Processing throughput (items per second)
- Scalability characteristics

**Expected results:**

- Memory allocations should grow linearly with item count
- Processing overhead should be consistent across sizes
- Demonstrates streaming's efficiency for large datasets

## Interpreting Results

BenchmarkDotNet outputs include:

- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Ratio**: Performance relative to baseline (when specified)
- **Gen0/Gen1/Gen2**: Garbage collection counts (lower is better)
- **Allocated**: Total memory allocated

### Key Metrics to Watch

1. **Memory Efficiency**: Lower "Allocated" values indicate better memory usage
2. **Throughput**: Higher operations/second or lower Mean time is better
3. **GC Pressure**: Lower Gen0/Gen1/Gen2 counts mean less GC overhead
4. **Consistency**: Lower StdDev means more predictable performance

## Tips for Running Benchmarks

1. **Always use Release mode** (`-c Release`) - Debug builds are not representative
2. **Close other applications** to reduce noise in measurements
3. **Run on battery power** (laptops) or disable power management for consistent results
4. **Multiple runs**: Run benchmarks multiple times to verify consistency
5. **Warm-up**: BenchmarkDotNet handles warm-up automatically

## Adding New Benchmarks

1. Create a new class with `[MemoryDiagnoser]` attribute
2. Add `[Benchmark]` methods for scenarios to test
3. Use `[Params]` for parameterized benchmarks
4. Add `[Baseline = true]` to one method for relative comparisons
5. Register in `Program.cs` with `BenchmarkRunner.Run<YourBenchmark>()`
