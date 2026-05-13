using System.Diagnostics;
using RinhaApi.Controllers.Dtos;
using RinhaApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVector, Vector>();
builder.Services.AddScoped<IMatrix, Matrix>();

var process = Process.GetCurrentProcess();

Console.WriteLine($"Loading preprocessed data...");
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");

// Load preprocessed data
var preprocessedDataPath = "./preprocessed-data.bin";
if (!File.Exists(preprocessedDataPath))
{
    Console.Error.WriteLine($"Error: {preprocessedDataPath} not found!");
    Console.Error.WriteLine("Make sure to run the DataPreprocessor tool first.");
    Environment.Exit(1);
}

var data = PreprocessedDataLoader.Load(preprocessedDataPath);

Console.WriteLine($"Loaded {data.Count} preprocessed references");
Console.WriteLine($"Grid cells: {data.Grid.Count}, avg bucket size: {data.Count / (float)data.Grid.Count:F1}");
Console.WriteLine($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
Console.WriteLine($"Working set: {process.WorkingSet64 / (1024 * 1024)} MB");

var vectorService = new Vector();

var fraudService = new FraudDetectionService(
    data.Labels,
    data.Vectors,
    data.VectorSize,
    data.BitsPerDim,
    data.GridDims,
    vectorService,
    data.Grid
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