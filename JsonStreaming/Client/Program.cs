// See https://aka.ms/new-console-template for more information


using System.Net.Http.Json;
using System.Text.Json.Serialization;


var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5192")
};

var streamItems = httpClient.GetFromJsonAsAsyncEnumerable<StreamItem>("/stream?startId=5");

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
