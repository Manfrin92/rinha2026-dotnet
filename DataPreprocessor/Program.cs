using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

var process = Process.GetCurrentProcess();
var vectorSize = 14;
var gridDims = new[] { 0, 1, 2, 3, 4, 7, 8 }; // continuous dims only, skips -1 flags (5,6) and binaries (9,10,11)
var bitsPerDim = 3;
int capacity = 3_000_000;

var vectors = new byte[capacity * vectorSize];
var labels = new byte[capacity];
var grid = new Dictionary<long, List<int>>();

int index = 0;

Console.WriteLine($"Starting data preprocessing...");
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");

var referencePath = "./references.json.gz";
if (!File.Exists(referencePath))
{
    Console.Error.WriteLine($"Error: {referencePath} not found!");
    Environment.Exit(1);
}

using var gz = new GZipStream(File.OpenRead(referencePath), CompressionMode.Decompress);
await foreach (var r in JsonSerializer.DeserializeAsyncEnumerable(gz, RefJsonContext.Default.Reference))
{
    int baseOffset = index * vectorSize;

    for (int i = 0; i < vectorSize; i++)
    {
        var value = r?.Vector[i];
        if (value < 0) value = 0;
        if (value > 1) value = 1;
        if (value != null)
            vectors[baseOffset + i] = (byte)(value * 255);
    }

    labels[index] = r?.Label == "fraud" ? (byte)1 : (byte)0;

    long key = GetGridKey(vectors, baseOffset, gridDims, bitsPerDim);
    if (!grid.TryGetValue(key, out var bucket))
        grid[key] = bucket = new List<int>(64);
    bucket.Add(index);

    index++;
}

Console.WriteLine($"Processed {index} references");
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");
Console.WriteLine($"Grid cells: {grid.Count}, avg bucket size: {index / (float)grid.Count:F1}");

var outputPath = "./preprocessed-data.bin";
Console.WriteLine($"Saving preprocessed data to {outputPath}...");

using var fs = File.Create(outputPath);
using var writer = new BinaryWriter(fs);

// Write header
writer.Write("RINHA");
writer.Write(1); // version
writer.Write(vectorSize);
writer.Write(bitsPerDim);
writer.Write(index);

// Write gridDims
writer.Write(gridDims.Length);
foreach (var dim in gridDims)
    writer.Write(dim);

// Write vectors
writer.Write(vectors.Length);
writer.Write(vectors);

// Write labels
writer.Write(labels.Length);
writer.Write(labels);

// Write grid
writer.Write(grid.Count);
foreach (var kvp in grid)
{
    writer.Write(kvp.Key);
    writer.Write(kvp.Value.Count);
    foreach (var bucketItem in kvp.Value)
        writer.Write(bucketItem);
}

Console.WriteLine($"Preprocessed data saved successfully!");
Console.WriteLine($"Final working set: {process.WorkingSet64 / (1024 * 1024)} MB");

static long GetGridKey(byte[] vectors, int baseOffset, int[] gridDims, int bitsPerDim)
{
    long key = 0;
    for (int i = 0; i < gridDims.Length; i++)
        key |= (long)(vectors[baseOffset + gridDims[i]] >> (8 - bitsPerDim)) << (i * bitsPerDim);
    return key;
}

public record Reference(
    [property: JsonPropertyName("vector")] double[] Vector,
    [property: JsonPropertyName("label")] string Label);

[JsonSerializable(typeof(List<Reference>))]
internal partial class RefJsonContext : JsonSerializerContext { }