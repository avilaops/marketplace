using MarketplaceBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<StorefrontConfig> StorefrontConfigs => Set<StorefrontConfig>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(e => e.Slug).IsUnique();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Domain
        modelBuilder.Entity<Domain>(entity =>
        {
            entity.ToTable("domains");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Hostname)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.Hostname).IsUnique();
            entity.HasIndex(e => e.TenantId);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Domains)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StorefrontConfig
        modelBuilder.Entity<StorefrontConfig>(entity =>
        {
            entity.ToTable("storefront_configs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.StoreName)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Subdomain)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);
            
            entity.Property(e => e.Locale)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(e => e.Theme)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.HasIndex(e => e.Subdomain);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Tenant)
                .WithOne(t => t.StorefrontConfig)
                .HasForeignKey<StorefrontConfig>(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Entity)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.UserId)
                .HasMaxLength(100);
            
            entity.Property(e => e.UserEmail)
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Entity, e.EntityId });
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
