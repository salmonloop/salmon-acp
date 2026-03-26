using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SalmonEgg.Domain.Models.Protocol;

public class SessionListParams
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtraParams { get; set; }
}

public class SessionListResponse
{
    [JsonPropertyName("sessions")]
    public List<AgentSessionInfo> Sessions { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, object>? ExtraData { get; set; }
}

public class AgentSessionInfo
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}