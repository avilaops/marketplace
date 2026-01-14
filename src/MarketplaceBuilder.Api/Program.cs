using Amazon.S3;
using MarketplaceBuilder.Api.Endpoints;
using MarketplaceBuilder.Api.Middleware;
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
