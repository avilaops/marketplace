using MarketplaceBuilder.Core.Interfaces;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MarketplaceBuilder.Infrastructure.Services;

public class TenantResolver : ITenantResolver
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    public Guid? TenantId { get; private set; }

    public TenantResolver(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Guid?> ResolveTenantAsync(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            TenantId = null;
            return null;
        }

        // Normalizar hostname (lowercase, remover porta)
        hostname = hostname.Split(':')[0].ToLowerInvariant();

        // Tentar cache primeiro
        var cacheKey = $"tenant:hostname:{hostname}";
        var cachedTenantId = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedTenantId) && Guid.TryParse(cachedTenantId, out var tenantIdFromCache))
        {
            TenantId = tenantIdFromCache;
            return tenantIdFromCache;
        }

        // Buscar no banco
        var domain = await _context.Domains
            .AsNoTracking()
            .Where(d => d.Hostname == hostname && d.IsActive)
            .Select(d => new { d.TenantId })
            .FirstOrDefaultAsync();

        if (domain != null)
        {
            // Armazenar no cache
            await _cache.SetStringAsync(
                cacheKey,
                domain.TenantId.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration
                });

            TenantId = domain.TenantId;
            return domain.TenantId;
        }

        TenantId = null;
        return null;
    }

    public static async Task InvalidateCacheAsync(IDistributedCache cache, string hostname)
    {
        hostname = hostname.Split(':')[0].ToLowerInvariant();
        var cacheKey = $"tenant:hostname:{hostname}";
        await cache.RemoveAsync(cacheKey);
    }
}
