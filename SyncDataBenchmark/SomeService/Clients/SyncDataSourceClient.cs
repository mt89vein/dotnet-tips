using System.Runtime.CompilerServices;

namespace SomeService.Clients;

public sealed class SyncDataSourceClient
{
    private const string ClientName = "SyncDataSource";
    private readonly IHttpClientFactory _httpClientFactory;

    public SyncDataSourceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Consuming typical offset paging API.
    /// </summary>
    public async IAsyncEnumerable<UserDto> GetUserStreamViaOffsetPagingAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var page = 1;

        using var client = _httpClientFactory.CreateClient(ClientName);

        while (!ct.IsCancellationRequested)
        {
            var qs = QueryString.Create([
                new KeyValuePair<string, string?>("page", page.ToString()),
                new KeyValuePair<string, string?>("pageSize", "1000")
            ]);

            var response = await client.GetFromJsonAsync<UserDto[]>($"/offset-paging{qs.ToString()}", ct);

            if (response is { Length: > 0 })
            {
                foreach (var user in response)
                {
                    yield return user;
                }

                page++;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Consuming optimized keyset paging server API.
    /// </summary>
    /// <remarks>
    /// <see href="https://use-the-index-luke.com/no-offset"/>.
    /// </remarks>
    public async IAsyncEnumerable<UserDto> GetUserStreamViaKeySetPagingAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        Guid? lastId = null;

        using var client = _httpClientFactory.CreateClient(ClientName);

        while (!ct.IsCancellationRequested)
        {
            var qs = QueryString.Create("pageSize", "1000");

            if (lastId.HasValue)
            {
                qs = qs.Add("lastId", lastId.Value.ToString());
            }

            var response = await client.GetFromJsonAsync<UserDto[]>($"keyset-paging{qs.ToString()}", ct);

            if (response is { Length: > 0 })
            {
                foreach (var user in response)
                {
                    yield return user;
                }

                var newLastId = response[^1].Id;

                if (newLastId == lastId)
                {
                    break;
                }
                lastId = newLastId;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Consuming JSON stream from server that fetched via entity framework core.
    /// </summary>
    public IAsyncEnumerable<UserDto> GetUserViaEfCoreStreamAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);

        return client.GetFromJsonAsAsyncEnumerable<UserDto>("ef-core-stream", ct);
    }

    /// <summary>
    /// Consuming super optimized server JSON stream via Npgsql (COPY TO STDOUT),
    /// without full buffering data in-memory.
    /// </summary>
    public IAsyncEnumerable<UserDto> GetUserViaNpgsqlStreamAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);

        return client.GetFromJsonAsAsyncEnumerable<UserDto>("npgsql-copy-to-stream", ct);
    }
}

public readonly record struct UserDto
{
    public Guid Id { get; init; }

    public required string Surname { get; init; }

    public required string Name { get; init; }

    public string? Patronymic { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public required bool IsActive { get; init; }
}
