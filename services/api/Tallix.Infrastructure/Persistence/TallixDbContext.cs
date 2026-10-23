using Microsoft.EntityFrameworkCore;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;
using Tallix.Domain.Users;

namespace Tallix.Infrastructure.Persistence;

public class TallixDbContext : DbContext
{
    public TallixDbContext(DbContextOptions<TallixDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in this project.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TallixDbContext).Assembly);
    }
}