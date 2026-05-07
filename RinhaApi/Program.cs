using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using RinhaApi.Controllers.Dtos;
using RinhaApi.Models;
using RinhaApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVector, Vector>();
builder.Services.AddScoped<IMatrix, Matrix>();

var process = Process.GetCurrentProcess();
var vectorSize = 5;
var bitsPerDim = 3; // 8 bins per dimension — tune this if buckets are too large/small
int capacity = 3_000_000;

var vectors = new byte[capacity * vectorSize];
var labels = new byte[capacity];
var grid = new Dictionary<long, List<int>>();

int index = 0;

Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");

using var gz = new GZipStream(File.OpenRead("./references.json.gz"), CompressionMode.Decompress);
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

    // Build grid index
    long key = GetGridKey(vectors, baseOffset, vectorSize, bitsPerDim);
    if (!grid.TryGetValue(key, out var bucket))
        grid[key] = bucket = new List<int>(64);
    bucket.Add(index);

    index++;
}

Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");
Console.WriteLine($"Grid cells: {grid.Count}, avg bucket size: {index / (float)grid.Count:F1}");

var vectorService = new Vector();

var fraudService = new FraudDetectionService(
    labels,
    vectors,
    vectorSize,
    bitsPerDim,
    vectorService,
    grid
);

builder.Services.AddSingleton<IFraudDetectionService>(fraudService);

builder.Logging.ClearProviders(); // removes all logging overhead per request

builder.Services.ConfigureHttpJsonOptions(opts => {
    opts.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});

var app = builder.Build();

app.MapPost("/fraud-score", (
    FraudScoreRequest request,
    IFraudDetectionService svc) => svc.IsFraudulent(request));

app.MapGet("/ready", (
    IFraudDetectionService svc) => svc.IsReady() ? Results.Ok() : Results.StatusCode(503)); 

app.Run();

static long GetGridKey(byte[] vectors, int baseOffset, int vectorSize, int bitsPerDim)
{
    long key = 0;
    for (int i = 0; i < vectorSize; i++)
        key |= (long)(vectors[baseOffset + i] >> (8 - bitsPerDim)) << (i * bitsPerDim);
    return key;
}