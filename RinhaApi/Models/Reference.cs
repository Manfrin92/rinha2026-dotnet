using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace RinhaApi.Models;

public record Reference(
    [property: JsonPropertyName("vector")] double[] Vector,
    [property: JsonPropertyName("label")] string Label);

[JsonSerializable(typeof(List<Reference>))]
internal partial class RefJsonContext : JsonSerializerContext
{

}