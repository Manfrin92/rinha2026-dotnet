using RinhaApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();

var app = builder.Build();

app.MapControllers(); 

app.Run();
