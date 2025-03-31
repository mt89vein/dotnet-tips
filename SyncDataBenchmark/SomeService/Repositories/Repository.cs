using System.Runtime.CompilerServices;
using EFCore.BulkExtensions;
using LinqToDB.EntityFrameworkCore;
using Npgsql;
using SomeService.Clients;

namespace SomeService;

public sealed class Repository
{
    private readonly ServiceContext _ctx;
    private readonly NpgsqlDataSource _npgsqlDataSource;

    public Repository(ServiceContext ctx, NpgsqlDataSource npgsqlDataSource)
    {
        _ctx = ctx;
        _npgsqlDataSource = npgsqlDataSource;
    }

    public async Task SaveEfTypicalAsync(IAsyncEnumerable<UserDto> stream, CancellationToken ct = default)
    {
        var count = 0;
        const int batchSize = 5000;
        _ctx.ChangeTracker.AutoDetectChangesEnabled = false;
        await foreach (var user in stream.WithCancellation(ct))
        {
            _ctx.Users.Add(Map(user));

            count++;

            if (count >= batchSize)
            {
                await _ctx.SaveChangesAsync(ct);
                count = 0;
            }
        }
    }

    public async Task SaveEfBulkAsync(IAsyncEnumerable<UserDto> stream, CancellationToken ct = default)
    {
        _ctx.ChangeTracker.AutoDetectChangesEnabled = false;

        const int batchSize = 5000;

        var buffer = new List<User>(batchSize);
        await foreach (var user in stream.WithCancellation(ct))
        {
            buffer.Add(Map(user));
            if (buffer.Count >= batchSize)
            {
                await _ctx.BulkInsertAsync(buffer, cancellationToken: ct);
                await _ctx.BulkSaveChangesAsync(cancellationToken: ct);
                buffer.Clear();
            }
        }
    }

    public async Task SaveLinq2DbAsync(IAsyncEnumerable<UserDto> stream, CancellationToken ct = default)
    {
        await _ctx.BulkCopyAsync(MapStream(stream, ct), ct);

        return;

        static async IAsyncEnumerable<User> MapStream(IAsyncEnumerable<UserDto> stream, [EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var user in stream.WithCancellation(ct))
            {
                yield return Map(user);
            }
        }
    }

    public async Task SaveNpgsqlCopyToAsync(IAsyncEnumerable<UserDto> stream, CancellationToken ct = default)
    {
        await using var conn = await _npgsqlDataSource.OpenConnectionAsync(ct);

        // max batch size?

        await using var importer = await conn.BeginBinaryImportAsync("COPY users (id, surname, name, patronymic, created_at, is_active) FROM STDIN (FORMAT BINARY)", ct);
        await foreach (var user in stream.WithCancellation(ct))
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(user.Id, ct);
            await importer.WriteAsync(user.Surname, ct);
            await importer.WriteAsync(user.Name, ct);
            await importer.WriteAsync(user.Patronymic, ct);
            await importer.WriteAsync(user.CreatedAt, ct);
            await importer.WriteAsync(user.IsActive, ct);
        }
        await importer.CompleteAsync(ct);
    }

    public async Task ClearUsersAsync(CancellationToken ct = default)
    {
        await using var conn = await _npgsqlDataSource.OpenConnectionAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "TRUNCATE TABLE users;";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static User Map(UserDto user)
    {
        return new User
        {
            Id = user.Id,
            Surname = user.Surname,
            Name = user.Name,
            Patronymic = user.Patronymic,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
        };
    }
}
