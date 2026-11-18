#nullable enable
using Microsoft.EntityFrameworkCore;
namespace Acme.Ticketing.Infrastructure;
public partial class TicketingDbContext(DbContextOptions<TicketingDbContext> options) : DbContext(options)
{
    // Object [AG][EN]
    public DbSet<Domain.Venue> Venues => Set<Domain.Venue>();

    public DbSet<Domain.Show> Shows => Set<Domain.Show>();

    public DbSet<Domain.TicketTier> TicketTiers => Set<Domain.TicketTier>();

    public DbSet<Domain.Booking> Bookings => Set<Domain.Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Object [AG][EN]
        modelBuilder.Entity<Domain.Venue>(builder =>
        {
            builder.HasKey(x => x.Id);
            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            builder.Property(x => x.Name).HasMaxLength(100);

            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_IS_UNIQUE
            builder.HasIndex(x => x.Name).IsUnique();

            // Property [M][NO][NAR][VO]
            builder.Property(x => x.Address).HasJsonConversion();

        });

        modelBuilder.Entity<Domain.Show>(builder =>
        {
            builder.HasKey(x => x.Id);
            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            builder.Property(x => x.Title).HasMaxLength(200);

            // Property [S][NO][NAR]

            // Property [E][NO][NAR]
            builder.Property(x => x.Status).HasConversion<string>();

            // Property [S][CA][NAR]
            builder.Ignore(x => x.IsSoldOut);

        });

        modelBuilder.Entity<Domain.TicketTier>(builder =>
        {
            builder.HasKey(x => x.Id);
            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            builder.Property(x => x.Name).HasMaxLength(50);

            // Property [S][NO][NAR]

        });

        modelBuilder.Entity<Domain.Booking>(builder =>
        {
            builder.HasKey(x => x.Id);
            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            builder.Property(x => x.Tier).HasMaxLength(50);

            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            builder.Property(x => x.CustomerEmail).HasMaxLength(100);

            // Property [S][NO][NAR]

        });

        OnModelCreatingPartial(modelBuilder);
        modelBuilder.ApplyDeleteRules();
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}