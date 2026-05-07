using System.Text.Json.Serialization;
using RinhaApi.Controllers.Dtos;

[JsonSerializable(typeof(FraudScoreRequest))]
[JsonSerializable(typeof(FraudScoreResponse))]
internal partial class AppJsonContext : JsonSerializerContext { }