using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Infrastructure.Services
{
    // Matriz plano -> features/limites: fonte unica de verdade da secao 4.2 do plano de produto.
    // Limites generosos por decisao do fundador (Rev. 5): creators 50/200/600, seats 10/30/ilimitado,
    // campanhas 15/60/ilimitado. Seats e campanhas nao tem custo marginal; creators e o driver de cobranca.
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
                    [PlanLimit.ActiveManagedCreators] = 50,
                    [PlanLimit.Seats] = 10,
                    [PlanLimit.ActiveCampaigns] = 15
                },
                [PlanTier.Pro] = new Dictionary<PlanLimit, int>
                {
                    [PlanLimit.ActiveManagedCreators] = 200,
                    [PlanLimit.Seats] = 30,
                    [PlanLimit.ActiveCampaigns] = 60
                },
                [PlanTier.Scale] = new Dictionary<PlanLimit, int>
                {
                    [PlanLimit.ActiveManagedCreators] = 600,
                    [PlanLimit.Seats] = Unlimited,
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
