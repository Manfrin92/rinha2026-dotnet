using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using RinhaApi.Models;
using RinhaApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<IVector, Vector>();
builder.Services.AddScoped<IMatrix, Matrix>();
var app = builder.Build();

var process = Process.GetCurrentProcess();
int legitCount = 0;
int fraudCount = 0;
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");

// Using Streams to read the file one by one and not loading everything in memory
using var gz = new GZipStream(File.OpenRead("./references.json.gz"), CompressionMode.Decompress);
await foreach (var r in JsonSerializer.DeserializeAsyncEnumerable<Reference>(
    gz, RefJsonContext.Default.Reference))
{
    if (r is null) continue;

    if (r.Label == "legit")
        legitCount++;
    else if (r.Label == "fraud")
        fraudCount++;
}

Console.WriteLine($"Loaded {legitCount+fraudCount} references.");
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");
Console.WriteLine($"Legit: {legitCount}, Fraud: {fraudCount}");

app.MapControllers(); 

app.Run();
