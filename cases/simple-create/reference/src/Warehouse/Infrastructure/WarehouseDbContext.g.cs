#nullable enable
using Microsoft.EntityFrameworkCore;
namespace Acme.Warehouse.Infrastructure;
public partial class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    // Object [AG]
    public DbSet<Domain.Supplier> Suppliers => Set<Domain.Supplier>();

    public DbSet<Domain.Product> Products => Set<Domain.Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Object [AG]
        modelBuilder.Entity<Domain.Supplier>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            entity.Property(x => x.Name).HasMaxLength(100);

            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            entity.Property(x => x.Email).HasMaxLength(100);

            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_IS_UNIQUE
            entity.HasIndex(x => x.Name).IsUnique();

        });

        modelBuilder.Entity<Domain.Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            entity.Property(x => x.Sku).HasMaxLength(20);

            // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
            entity.Property(x => x.Name).HasMaxLength(100);

            // Property [S][NO][NAR]
            // VALIDATION RULE - PROPERTY_IS_UNIQUE
            entity.HasIndex(x => x.Sku).IsUnique();

            // Property [E][NO][NAR]
            entity.Property(x => x.Status).HasConversion<string>();

            // Property [S][CA][NAR]
            entity.Ignore(x => x.NeedsReorder);

        });

        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}