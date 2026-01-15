using Amazon.S3;
using MarketplaceBuilder.Api.Endpoints;
using MarketplaceBuilder.Api.Middleware;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Core.Interfaces;
using MarketplaceBuilder.Infrastructure.Data;
using MarketplaceBuilder.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MarketplaceBuilder:";
});

// Add S3 client (MinIO compatible)
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["Storage:Endpoint"],
        ForcePathStyle = builder.Configuration.GetValue<bool>("Storage:ForcePathStyle"),
        SignatureVersion = "v4"
    };

    return new AmazonS3Client(
        builder.Configuration["Storage:AccessKey"],
        builder.Configuration["Storage:SecretKey"],
        config);
});

// Add storage service
builder.Services.AddScoped<IObjectStorage, S3StorageService>();

// Add Stripe gateway
builder.Services.AddScoped<IStripeGateway, StripeGatewayService>();

// Add tenant resolver
builder.Services.AddScoped<ITenantResolver, TenantResolver>();

// Add services to the container
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new[] { "db", "sql", "postgres" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "cache", "redis" });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations and seed data in Development/CI environments
using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Auto-migrate in Development, CI, or when explicitly enabled
    if (env.IsDevelopment() || 
        env.IsEnvironment("CI") || 
        config.GetValue<bool>("MigrateOnStartup"))
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Seed localhost domain for testing
            if (!await db.Domains.AnyAsync(d => d.Hostname == "localhost"))
            {
                logger.LogInformation("Seeding localhost domain for testing...");
                
                var testTenant = new Tenant
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Test Tenant",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var localhostDomain = new Domain
                {
                    Id = Guid.NewGuid(),
                    TenantId = testTenant.Id,
                    Hostname = "localhost",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var storefrontConfig = new StorefrontConfig
                {
                    Id = Guid.NewGuid(),
                    TenantId = testTenant.Id,
                    StoreName = "Test Store",
                    Currency = "USD",
                    Locale = "en-US",
                    Status = StorefrontStatus.Live,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.Tenants.Add(testTenant);
                db.Domains.Add(localhostDomain);
                db.StorefrontConfigs.Add(storefrontConfig);

                await db.SaveChangesAsync();
                logger.LogInformation("Localhost domain seeded successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during migration/seed");
            throw;
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Disable HTTPS redirection in CI/Test environments
if (!app.Environment.IsEnvironment("CI"))
{
    app.UseHttpsRedirection();
}


// Add tenant resolver middleware
app.UseTenantResolver();

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () => new 
{ 
    service = "MarketplaceBuilder API",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
});

// Map store provisioning endpoints
app.MapStoreProvisioningEndpoints();

// Map catalog endpoints
app.MapCategoryEndpoints();
app.MapProductEndpoints();
app.MapProductVariantEndpoints();
app.MapProductImageEndpoints();

// Map checkout endpoints
app.MapCheckoutEndpoints();
app.MapOrderEndpoints();

// Map webhook endpoints
app.MapWebhookEndpoints();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
