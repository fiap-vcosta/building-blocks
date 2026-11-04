using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildingBlocks.Events;

public static class EventJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        return options;
    }

    public static string Serialize<TData>(EventEnvelope<TData> envelope) =>
        JsonSerializer.Serialize(envelope, Options);

    public static EventEnvelope<TData>? Deserialize<TData>(string json) =>
        JsonSerializer.Deserialize<EventEnvelope<TData>>(json, Options);
}
