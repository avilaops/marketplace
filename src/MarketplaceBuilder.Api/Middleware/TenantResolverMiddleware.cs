using MarketplaceBuilder.Core.Interfaces;

namespace MarketplaceBuilder.Api.Middleware;

/// <summary>
/// Middleware que resolve o tenant baseado no Host header da requisição
/// </summary>
public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolverMiddleware> _logger;

    public TenantResolverMiddleware(RequestDelegate next, ILogger<TenantResolverMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        // Skip tenant resolution for test environment
        if (context.RequestServices.GetRequiredService<IHostEnvironment>().EnvironmentName == "Test")
        {
            await _next(context);
            return;
        }

        var hostname = context.Request.Host.Host;

        try
        {
            var tenantId = await tenantResolver.ResolveTenantAsync(hostname);

            if (tenantId.HasValue)
            {
                context.Items["TenantId"] = tenantId.Value;
                _logger.LogDebug("Resolved tenant {TenantId} for hostname {Hostname}", tenantId.Value, hostname);
            }
            else
            {
                _logger.LogDebug("No tenant found for hostname {Hostname}", hostname);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant for hostname {Hostname}", hostname);
        }

        await _next(context);
    }
}

public static class TenantResolverMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolver(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantResolverMiddleware>();
    }
}
