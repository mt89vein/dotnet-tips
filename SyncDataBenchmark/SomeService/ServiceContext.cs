using Microsoft.EntityFrameworkCore;

namespace SomeService;

public sealed class ServiceContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public ServiceContext(DbContextOptions<ServiceContext> options) : base(options) { }
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

