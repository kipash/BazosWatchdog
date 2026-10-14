using BazosWatchdog.Models;
using Microsoft.EntityFrameworkCore;

namespace BazosWatchdog.Data;

public sealed class WatchdogDbContext(DbContextOptions<WatchdogDbContext> options) : DbContext(options)
{
    public DbSet<UserQuery> UserQueries => Set<UserQuery>();
    public DbSet<QueryResultEntry> QueryResultEntries => Set<QueryResultEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserQuery>(entity =>
        {
            entity.Property(query => query.Name).HasMaxLength(120).IsRequired();
            entity.Property(query => query.SearchUrl).HasMaxLength(2_000).IsRequired();
            entity.HasIndex(query => query.SearchUrl).IsUnique();
        });

        modelBuilder.Entity<QueryResultEntry>(entity =>
        {
            entity.Property(result => result.Title).HasMaxLength(500).IsRequired();
            entity.Property(result => result.NormalizedTitle).HasMaxLength(500).IsRequired();
            entity.Property(result => result.ListingUrl).HasMaxLength(2_000).IsRequired();
            entity.HasIndex(result => new { result.UserQueryId, result.NormalizedTitle }).IsUnique();
            entity.HasIndex(result => result.FirstSeenAt);
        });
    }
}
