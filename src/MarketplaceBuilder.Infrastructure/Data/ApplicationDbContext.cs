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
    
    // Catalog
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    
    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();

    // AI
    public DbSet<TenantAiSettings> TenantAiSettings => Set<TenantAiSettings>();
    public DbSet<AiPrompt> AiPrompts => Set<AiPrompt>();
    public DbSet<AiRun> AiRuns => Set<AiRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Skip relational configurations for in-memory database
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            // Configure basic keys for in-memory database
            modelBuilder.Entity<Tenant>().HasKey(e => e.Id);
            modelBuilder.Entity<StorefrontConfig>().HasKey(e => e.Id);
            modelBuilder.Entity<AuditLog>().HasKey(e => e.Id);
            modelBuilder.Entity<TenantAiSettings>().HasKey(e => e.TenantId);
            modelBuilder.Entity<AiPrompt>().HasKey(e => e.Id);
            modelBuilder.Entity<AiRun>().HasKey(e => e.Id);
            return;
        }

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

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            entity.HasIndex(e => e.TenantId);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(300);
            
            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasMaxLength(5000);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(e => e.PrimaryImageUrl)
                .HasMaxLength(500);
            
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ProductVariant
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variants");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Sku)
                .HasMaxLength(100);
            
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);
            
            entity.HasIndex(e => new { e.TenantId, e.ProductId });
            entity.HasIndex(e => e.Sku);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductImage
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ObjectKey)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.PublicUrl)
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(e => e.ContentType)
                .HasMaxLength(100);
            
            entity.HasIndex(e => new { e.TenantId, e.ProductId });
            entity.HasIndex(e => e.ObjectKey);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);
            
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(255);
            
            entity.Property(e => e.StripeCheckoutSessionId)
                .HasMaxLength(255);
            
            entity.Property(e => e.StripePaymentIntentId)
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt });
            entity.HasIndex(e => e.StripeCheckoutSessionId);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TitleSnapshot)
                .IsRequired()
                .HasMaxLength(300);
            
            entity.Property(e => e.SkuSnapshot)
                .HasMaxLength(100);
            
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);
            
            entity.HasIndex(e => new { e.TenantId, e.OrderId });
            entity.HasIndex(e => e.OrderId);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StripeWebhookEvent
        modelBuilder.Entity<StripeWebhookEvent>(entity =>
        {
            entity.ToTable("stripe_webhook_events");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.StripeEventId)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.ProcessingStatus)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(e => e.Error)
                .HasMaxLength(2000);
            
            entity.HasIndex(e => e.StripeEventId).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ReceivedAt);
            
            entity.Property(e => e.ReceivedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // TenantAiSettings
        modelBuilder.Entity<TenantAiSettings>(entity =>
        {
            entity.ToTable("tenant_ai_settings");
            entity.HasKey(e => e.TenantId);
            
            entity.Property(e => e.ModelDefault)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.ApiKey)
                .HasMaxLength(255);
            
            entity.HasOne(e => e.Tenant)
                .WithOne()
                .HasForeignKey<TenantAiSettings>(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AiPrompt
        modelBuilder.Entity<AiPrompt>(entity =>
        {
            entity.ToTable("ai_prompts");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Template)
                .IsRequired();
            
            entity.Property(e => e.VariablesSchema)
                .IsRequired();
            
            entity.Property(e => e.Channel)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.HasIndex(e => new { e.Name, e.Version }).IsUnique();
        });

        // AiRun
        modelBuilder.Entity<AiRun>(entity =>
        {
            entity.ToTable("ai_runs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.InputHash)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.Output)
                .IsRequired();
            
            entity.Property(e => e.Model)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.CorrelationId)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Prompt)
                .WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}


