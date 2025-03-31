using System.Diagnostics;
using SomeService.Clients;

namespace SomeService;

public enum InsertApproachType
{
    Undefined = 0,
    EFTypical = 1,
    EFBulk = 2,
    Linq2DB = 3,
    NpgsqlCopy = 4,
}

public enum FetchApproachType
{
    Undefined = 0,
    EFOffsetPaging = 1,
    EFKeysetPaging = 2,
    EFStream = 3,
    NpgsqlCopy = 4,
}

public sealed class Runner
{
    private readonly SyncDataSourceClient _client;
    private readonly Repository _repository;
    private readonly ILogger<Runner> _logger;

    public Runner(
        SyncDataSourceClient client,
        Repository repository,
        ILogger<Runner> logger
    )
    {
        _client = client;
        _repository = repository;
        _logger = logger;
    }

    public async Task RunAsync(
        InsertApproachType insertApproachType,
        FetchApproachType fetchApproachType,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation("Sync requested as {InsertType} {FetchType}", insertApproachType, fetchApproachType);

        await _repository.ClearUsersAsync(ct);

        _logger.LogInformation("Users table cleared");

        var sw = Stopwatch.StartNew();

        var usersStream = (fetchApproachType) switch
        {
            FetchApproachType.EFOffsetPaging => _client.GetUserStreamViaOffsetPagingAsync(ct),
            FetchApproachType.EFKeysetPaging => _client.GetUserStreamViaKeySetPagingAsync(ct),
            FetchApproachType.EFStream => _client.GetUserViaEfCoreStreamAsync(ct),
            FetchApproachType.NpgsqlCopy => _client.GetUserViaNpgsqlStreamAsync(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(fetchApproachType), fetchApproachType, null)
        };

        var insertTask = (insertApproachType) switch
        {
            InsertApproachType.EFTypical => _repository.SaveEfTypicalAsync(usersStream, ct),
            InsertApproachType.EFBulk => _repository.SaveEfBulkAsync(usersStream, ct),
            InsertApproachType.Linq2DB => _repository.SaveLinq2DbAsync(usersStream, ct),
            InsertApproachType.NpgsqlCopy => _repository.SaveNpgsqlCopyToAsync(usersStream, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(insertApproachType), insertApproachType, null)
        };

        await insertTask;

        sw.Stop();

        _logger.LogInformation("{InsertType} {FetchType}. Total Elapsed: {Elapsed} ms", insertApproachType, fetchApproachType, sw.ElapsedMilliseconds);
    }
}
