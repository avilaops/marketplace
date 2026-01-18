using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Core.Interfaces;
using MarketplaceBuilder.Infrastructure.Bootstrapping;
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

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
