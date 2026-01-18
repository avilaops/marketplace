using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketplaceBuilder.Infrastructure.Bootstrapping;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        try
        {
            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                logger.LogInformation("Applying database migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                logger.LogInformation("Skipping migrations for in-memory database");
            }

            // Seed minimum data if not exists
            await SeedMinimumDataAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database initialization");
            throw;
        }
    }

    private static async Task SeedMinimumDataAsync(ApplicationDbContext db, ILogger logger)
    {
        // Seed default tenant if not exists
        if (!await db.Tenants.AnyAsync())
        {
            logger.LogInformation("Seeding default tenant...");

            var defaultTenant = new Tenant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Default Tenant",
                Slug = "default",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Tenants.Add(defaultTenant);
            await db.SaveChangesAsync();
            logger.LogInformation("Default tenant seeded successfully");
        }

        // Seed localhost domain if not exists
        if (!await db.Domains.AnyAsync(d => d.Hostname == "localhost"))
        {
            logger.LogInformation("Seeding localhost domain...");

            var tenant = await db.Tenants.FirstAsync();

            var localhostDomain = new Domain
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Hostname = "localhost",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Domains.Add(localhostDomain);
            await db.SaveChangesAsync();
            logger.LogInformation("Localhost domain seeded successfully");
        }

        // Seed additional test domains if needed
        var additionalHosts = new[] { "127.0.0.1", "*.localtest.me" };
        foreach (var host in additionalHosts)
        {
            if (!await db.Domains.AnyAsync(d => d.Hostname == host))
            {
                logger.LogInformation($"Seeding {host} domain...");

                var tenant = await db.Tenants.FirstAsync();

                var domain = new Domain
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Hostname = host,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.Domains.Add(domain);
            }
        }

        await db.SaveChangesAsync();

        // Seed default AI prompt if not exists
        if (!await db.AiPrompts.AnyAsync(p => p.Name == "generate-product-description"))
        {
            logger.LogInformation("Seeding default AI prompt...");

            var prompt = new AiPrompt
            {
                Name = "generate-product-description",
                Version = 1,
                Template = "Gere uma descrição atraente para o produto {{nome}} na categoria {{categoria}}. Mantenha em {{idioma}}.",
                VariablesSchema = "{\"nome\": \"string\", \"categoria\": \"string\", \"idioma\": \"string\"}",
                Channel = "admin"
            };

            db.AiPrompts.Add(prompt);
            await db.SaveChangesAsync();
            logger.LogInformation("Default AI prompt seeded successfully");
        }
    }
}