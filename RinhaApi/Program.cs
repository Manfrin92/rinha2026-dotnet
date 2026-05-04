using RinhaApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<IVector, Vector>();
builder.Services.AddScoped<IMatrix, Matrix>();
var app = builder.Build();

app.MapControllers(); 

app.Run();
