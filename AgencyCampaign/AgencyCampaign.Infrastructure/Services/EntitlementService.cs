using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Infrastructure.Services
{
    // Matriz plano -> features/limites: fonte unica de verdade da secao 4.2 do plano de produto.
    // Limites usam os valores do documento (Essencial 25/3/5, Pro 100/10/30, Scale 400/30/ilimitado).
    // A Revisao 4 recomenda avaliar Essencial -> 15 creators e 8-10 campanhas; basta ajustar aqui.
    public sealed class EntitlementService : IEntitlementService
    {
        private const int Unlimited = IEntitlementService.Unlimited;

        private static readonly IReadOnlyDictionary<PlanTier, IReadOnlySet<PlanFeature>> FeaturesByTier =
            new Dictionary<PlanTier, IReadOnlySet<PlanFeature>>
            {
                [PlanTier.Essencial] = new HashSet<PlanFeature>(),
                [PlanTier.Pro] = new HashSet<PlanFeature>
                {
                    PlanFeature.DigitalSignature,
                    PlanFeature.PixPayout,
                    PlanFeature.Automations,
                    PlanFeature.Portals,
                    PlanFeature.CommercialAnalytics,
                    PlanFeature.ApprovalPolicy,
                    PlanFeature.ProposalEngagementTracking
                },
                [PlanTier.Scale] = new HashSet<PlanFeature>
                {
                    PlanFeature.DigitalSignature,
                    PlanFeature.PixPayout,
                    PlanFeature.Automations,
                    PlanFeature.Portals,
                    PlanFeature.CommercialAnalytics,
                    PlanFeature.ApprovalPolicy,
                    PlanFeature.ProposalEngagementTracking,
                    PlanFeature.ApifySync,
                    PlanFeature.EmvRoi,
                    PlanFeature.ContentLicensing,
                    PlanFeature.PixGovernance,
                    PlanFeature.AdvancedFinancialReports
                }
            };

        private static readonly IReadOnlyDictionary<PlanTier, IReadOnlyDictionary<PlanLimit, int>> LimitsByTier =
            new Dictionary<PlanTier, IReadOnlyDictionary<PlanLimit, int>>
            {
                [PlanTier.Essencial] = new Dictionary<PlanLimit, int>
                {
                    [PlanLimit.ActiveManagedCreators] = 25,
                    [PlanLimit.Seats] = 3,
                    [PlanLimit.ActiveCampaigns] = 5
                },
                [PlanTier.Pro] = new Dictionary<PlanLimit, int>
                {
                    [PlanLimit.ActiveManagedCreators] = 100,
                    [PlanLimit.Seats] = 10,
                    [PlanLimit.ActiveCampaigns] = 30
                },
                [PlanTier.Scale] = new Dictionary<PlanLimit, int>
                {
                    [PlanLimit.ActiveManagedCreators] = 400,
                    [PlanLimit.Seats] = 30,
                    [PlanLimit.ActiveCampaigns] = Unlimited
                }
            };

        public bool HasFeature(PlanTier tier, PlanFeature feature)
        {
            if (tier == PlanTier.Internal)
            {
                return true;
            }

            return FeaturesByTier.TryGetValue(tier, out IReadOnlySet<PlanFeature>? features) && features.Contains(feature);
        }

        public int GetLimit(PlanTier tier, PlanLimit limit)
        {
            if (tier == PlanTier.Internal)
            {
                return Unlimited;
            }

            if (LimitsByTier.TryGetValue(tier, out IReadOnlyDictionary<PlanLimit, int>? limits) &&
                limits.TryGetValue(limit, out int value))
            {
                return value;
            }

            return 0;
        }

        public EntitlementCheck CheckLimit(PlanTier tier, PlanLimit limit, int currentUsage)
        {
            int max = GetLimit(tier, limit);
            bool allowed = currentUsage < max;

            return new EntitlementCheck(allowed, max, currentUsage);
        }
    }
}
