using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Services
{
    // Resolve o tier do tenant atual (lido do cache em AgencySettings). E o que os pontos de gate consultam
    // antes de chamar o IEntitlementService. Sem tier atribuido = Internal (fail-open).
    public interface IPlanTierResolver
    {
        Task<PlanTier> GetCurrentTierAsync(CancellationToken cancellationToken = default);
    }
}
