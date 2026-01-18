using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;

namespace MarketplaceBuilder.Infrastructure.AI;

public class AiUsageRecorder
{
    private readonly ApplicationDbContext _db;

    public AiUsageRecorder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(Guid tenantId, int promptId, string inputHash, AiRunResult result, string correlationId)
    {
        var run = new AiRun
        {
            TenantId = tenantId,
            PromptId = promptId,
            InputHash = inputHash,
            Output = result.Output,
            Model = "gpt-4o-mini", // Fixo por enquanto
            TokensUsed = result.TokensUsed,
            CostUsd = result.CostUsd,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        _db.AiRuns.Add(run);
        await _db.SaveChangesAsync();
    }
}