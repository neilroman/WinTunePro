using System.Text.Json.Serialization;

namespace WinTune.Broker.Contracts;

public sealed record RpcRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("payload")] string? PayloadJson
)
{
    public static RpcRequest Create(string method, object? payload = null) =>
        new(Guid.NewGuid().ToString(), method, payload is null ? null
            : System.Text.Json.JsonSerializer.Serialize(payload));
}

public sealed record RpcResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("result")] string? ResultJson,
    [property: JsonPropertyName("error")] string? Error
);
