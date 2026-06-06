using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Exceptions;

namespace AgencyCampaign.Infrastructure.Services
{
    // Gating por plano sobre IPlanTierResolver + IEntitlementService. Lanca ForbiddenException (403, mensagem
    // i18n "plan.feature.unavailable"/"plan.limit.reached") em acoes de usuario; HasFeatureAsync apenas consulta.
    // Nome termina em "Gate" (nao "Service"), entao e registrado manualmente na DI.
    public sealed class PlanGate : IPlanGate
    {
        private readonly IPlanTierResolver tierResolver;
        private readonly IEntitlementService entitlements;

        public PlanGate(IPlanTierResolver tierResolver, IEntitlementService entitlements)
        {
            this.tierResolver = tierResolver;
            this.entitlements = entitlements;
        }

        public async Task RequireFeatureAsync(PlanFeature feature, CancellationToken cancellationToken = default)
        {
            PlanTier tier = await tierResolver.GetCurrentTierAsync(cancellationToken);
            if (!entitlements.HasFeature(tier, feature))
            {
                throw new ForbiddenException("plan.feature.unavailable", feature);
            }
        }

        public async Task<bool> HasFeatureAsync(PlanFeature feature, CancellationToken cancellationToken = default)
        {
            PlanTier tier = await tierResolver.GetCurrentTierAsync(cancellationToken);
            return entitlements.HasFeature(tier, feature);
        }

        public async Task RequireWithinLimitAsync(PlanLimit limit, int currentUsage, CancellationToken cancellationToken = default)
        {
            PlanTier tier = await tierResolver.GetCurrentTierAsync(cancellationToken);
            EntitlementCheck check = entitlements.CheckLimit(tier, limit, currentUsage);
            if (!check.Allowed)
            {
                throw new ForbiddenException("plan.limit.reached", check.Limit);
            }
        }
    }
}
