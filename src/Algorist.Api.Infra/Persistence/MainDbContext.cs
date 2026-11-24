using Algorist.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Algorist.Api.Infra.Persistence;

/// <summary>EF Core context for the main SQL Server database.</summary>
public sealed class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserAvatar> UserAvatars => Set<UserAvatar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);
}
