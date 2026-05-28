using System.Globalization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class PolicyEvaluatorService : IPolicyEvaluator
    {
        private readonly DbContext dbContext;

        public PolicyEvaluatorService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<PolicyEvaluationModel> EvaluateProposalByIdAsync(long proposalId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await dbContext.Set<Proposal>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);

            if (proposal is null)
            {
                return new PolicyEvaluationModel { HasDeviations = false };
            }

            return await EvaluateProposalAsync(proposal, cancellationToken);
        }

        public async Task<PolicyEvaluationModel> EvaluateProposalAsync(Proposal proposal, CancellationToken cancellationToken = default)
        {
            CommercialPolicy? policy = await dbContext.Set<CommercialPolicy>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (policy is null)
            {
                return new PolicyEvaluationModel { HasDeviations = false, PolicyMissing = true };
            }

            // Sempre emite as linhas de comparacao (dentro = Kind 1; violacao = Kind 2),
            // para o aprovador ver todos os termos da proposta, nao so os que estouram a politica.
            List<PolicyDeviationModel> comparisons = [];
            List<PolicyImpactModel> impacts = [];

            if (policy.MaxDiscountPercent.HasValue && proposal.DiscountAmount.HasValue)
            {
                decimal requested = proposal.DiscountPercent;
                decimal max = policy.MaxDiscountPercent.Value;
                bool violates = requested > max;
                comparisons.Add(new PolicyDeviationModel
                {
                    Field = "Desconto",
                    PolicyValue = $"máx {FormatPercent(max)}",
                    RequestedValue = FormatPercent(requested),
                    Delta = violates ? $"+{FormatPercentPoints(requested - max)}" : "dentro",
                    Kind = violates ? 2 : 1,
                    IsViolation = violates,
                });

                if (violates)
                {
                    decimal lostRevenue = proposal.TotalValue * ((requested - max) / 100m);
                    impacts.Add(new PolicyImpactModel { Label = "Receita", Value = $"-{FormatBrl(lostRevenue)}", IsGood = false });
                }
            }

            if (policy.MaxPaymentTermDays.HasValue && proposal.PaymentTermDays.HasValue)
            {
                int requested = proposal.PaymentTermDays.Value;
                int max = policy.MaxPaymentTermDays.Value;
                bool violates = requested > max;
                comparisons.Add(new PolicyDeviationModel
                {
                    Field = "Prazo de pagamento",
                    PolicyValue = $"máx {max} dias",
                    RequestedValue = $"{requested} dias",
                    Delta = violates ? $"+{requested - max}d" : "dentro",
                    Kind = violates ? 2 : 1,
                    IsViolation = violates,
                });

                if (violates)
                {
                    impacts.Add(new PolicyImpactModel { Label = "Cashflow", Value = $"-{requested - max} dias", IsGood = false });
                }
            }

            string? suggestedType = null;
            if (comparisons.Any(d => d.IsViolation && d.Field == "Desconto"))
            {
                suggestedType = "discount";
            }
            else if (comparisons.Any(d => d.IsViolation && d.Field == "Prazo de pagamento"))
            {
                suggestedType = "deadline";
            }

            return new PolicyEvaluationModel
            {
                HasDeviations = comparisons.Any(d => d.IsViolation),
                PolicyMissing = false,
                SuggestedApprovalType = suggestedType,
                Deviations = comparisons,
                Impacts = impacts,
            };
        }

        private static string FormatPercent(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') + "%";

        private static string FormatPercentPoints(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') + "pp";

        private static string FormatBrl(decimal value) => value.ToString("C0", new CultureInfo("pt-BR"));
    }
}
