using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    // Le o tier em cache no AgencySettings (singleton por tenant; o DbContext ja vem escopado ao tenant).
    // Sem settings/tier = Internal (fail-open): nao gateia nada ate a assinatura atribuir o tier real.
    // Nome termina em "Resolver" (nao "Service"), entao e registrado manualmente na DI.
    public sealed class PlanTierResolver : IPlanTierResolver
    {
        private readonly DbContext dbContext;

        public PlanTierResolver(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<PlanTier> GetCurrentTierAsync(CancellationToken cancellationToken = default)
        {
            List<PlanTier> tiers = await dbContext.Set<AgencySettings>()
                .Select(settings => settings.PlanTier)
                .Take(1)
                .ToListAsync(cancellationToken);

            return tiers.Count > 0 ? tiers[0] : PlanTier.Internal;
        }
    }
}
