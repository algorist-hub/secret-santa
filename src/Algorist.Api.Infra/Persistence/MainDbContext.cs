using Microsoft.EntityFrameworkCore;

namespace Algorist.Api.Infra.Persistence;

/// <summary>
/// EF Core context for the main SQL Server database. No entities yet.
/// </summary>
public sealed class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options);
