using Microsoft.EntityFrameworkCore;

namespace SyncDataSource;

public sealed class UsersContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public UsersContext(DbContextOptions<UsersContext> options) : base(options) { }
}

public sealed class User
{
    public Guid Id { get; set; }

    public required string Surname { get; set; }

    public required string Name { get; set; }

    public string? Patronymic { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public required bool IsActive { get; set; }
}
