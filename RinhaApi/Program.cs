using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using RinhaApi.Models;
using RinhaApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IVector, Vector>();
builder.Services.AddScoped<IMatrix, Matrix>();

var process = Process.GetCurrentProcess();
var vectorSize = 5;
int capacity = 3_000_000;

var vectors = new byte[capacity * vectorSize];
var labels = new byte[capacity];

int index = 0;
int legitCount = 0;
int fraudCount = 0;
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");

// Using Streams to read the file one by one and not loading everything in memory
using var gz = new GZipStream(File.OpenRead("./references.json.gz"), CompressionMode.Decompress);
await foreach (var r in JsonSerializer.DeserializeAsyncEnumerable(gz, RefJsonContext.Default.Reference))
{
    // --- label ---
    if (r?.Label == "legit")
    {
        labels[index] = 0;
        legitCount++;
    }
    else
    {
        labels[index] = 1;
        fraudCount++;
    }

    // --- dimensionality reduction by truncation
    int baseOffset = index * vectorSize;

    for (int i = 0; i < vectorSize; i++)
    {
        var value = r?.Vector[i]; // double between 0–1

        // clamp just in case
        if (value < 0) value = 0;
        if (value > 1) value = 1;

        if (value != null)
        {
            vectors[baseOffset + i] = (byte)(value * 255);
        }
    }

    index++;
}

Console.WriteLine($"Loaded {legitCount+fraudCount} references.");
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");
Console.WriteLine($"Legit: {legitCount}, Fraud: {fraudCount}");

Console.WriteLine("labels size: {0}", labels.Length);
Console.WriteLine("vectors size: {0}", vectors.Length);

var vectorService = new Vector();

var fraudService = new FraudDetectionService(
    legitCount,
    fraudCount,
    labels,
    vectors,
    vectorSize,
    index,
    vectorService
);

builder.Services.AddSingleton<IFraudDetectionService>(fraudService);

var app = builder.Build();

app.MapControllers(); 

app.Run();
