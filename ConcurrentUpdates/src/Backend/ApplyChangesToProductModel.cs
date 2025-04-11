using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcurrentUpdates;

public sealed class ApplyChangesToProductModel
{
    public int Version { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement> Changes { get; set; }
}
