using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MarketplaceBuilder.Api;
using MarketplaceBuilder.Infrastructure.Data;

namespace MarketplaceBuilder.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext registrations
            var dbContextDescriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                     d.ServiceType == typeof(ApplicationDbContext)).ToList();
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });
        });
    }
}