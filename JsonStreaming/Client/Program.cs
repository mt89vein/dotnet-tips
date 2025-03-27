// See https://aka.ms/new-console-template for more information


using System.Text.Json;
using System.Text.Json.Serialization;


var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5192")
};

await using var responseStream = await httpClient.GetStreamAsync("/stream");

var streamItems = JsonSerializer.DeserializeAsyncEnumerable<StreamItem>(responseStream);

await foreach (var streamItem in streamItems)
{
    Console.WriteLine("Timestamp: {0:O} Value: {1}", streamItem.Timestamp, streamItem.Value);
}

public readonly record struct StreamItem
{
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }


    [JsonPropertyName("value")]
    public required int Value { get; init; }
}