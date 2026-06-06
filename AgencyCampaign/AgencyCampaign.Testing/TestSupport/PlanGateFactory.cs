using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;

namespace AgencyCampaign.Testing.TestSupport
{
    // Cria um IPlanGate com tier fixo para testes de service. Default Internal = acesso total (nao gateia),
    // entao testes que nao se importam com plano continuam passando sem mudanca de comportamento.
    public static class PlanGateFactory
    {
        private sealed class StubTierResolver : IPlanTierResolver
        {
            private readonly PlanTier tier;

            public StubTierResolver(PlanTier tier)
            {
                this.tier = tier;
            }

            public Task<PlanTier> GetCurrentTierAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(tier);
            }
        }

        public static IPlanGate Create(PlanTier tier = PlanTier.Internal)
        {
            return new PlanGate(new StubTierResolver(tier), new EntitlementService());
        }
    }
}
