using System.Text.Json.Serialization;

namespace MockHealthSystem.Api.Models.System;

public class ODataPageResult<T>
{
    [JsonPropertyName("Items")]
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    [JsonPropertyName("Count")]
    public long? Count { get; set; }

    [JsonPropertyName("NextPageLink")]
    public string? NextPageLink { get; set; }
}

