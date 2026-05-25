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

        public async Task<PolicyEvaluationModel> EvaluateNegotiationByIdAsync(long negotiationId, CancellationToken cancellationToken = default)
        {
            OpportunityNegotiation? negotiation = await dbContext.Set<OpportunityNegotiation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == negotiationId, cancellationToken);

            if (negotiation is null)
            {
                return new PolicyEvaluationModel { HasDeviations = false };
            }

            return await EvaluateNegotiationAsync(negotiation, cancellationToken);
        }

        public async Task<PolicyEvaluationModel> EvaluateNegotiationAsync(OpportunityNegotiation negotiation, CancellationToken cancellationToken = default)
        {
            CommercialPolicy? policy = await dbContext.Set<CommercialPolicy>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (policy is null)
            {
                return new PolicyEvaluationModel { HasDeviations = false, PolicyMissing = true };
            }

            // Sempre emite as linhas de comparacao (dentro = Kind 1; violacao = Kind 2/3),
            // para o aprovador ver todos os termos negociados, nao so os que estouram a politica.
            List<PolicyDeviationModel> comparisons = [];
            List<PolicyImpactModel> impacts = [];

            if (policy.MaxDiscountPercent.HasValue && negotiation.DiscountPercent.HasValue)
            {
                decimal requested = negotiation.DiscountPercent.Value;
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
                    decimal lostRevenue = negotiation.Amount * ((requested - max) / 100m);
                    impacts.Add(new PolicyImpactModel { Label = "Receita", Value = $"-{FormatBrl(lostRevenue)}", IsGood = false });
                }
            }

            if (policy.MinMarginPercent.HasValue && negotiation.MarginPercent.HasValue)
            {
                decimal requested = negotiation.MarginPercent.Value;
                decimal min = policy.MinMarginPercent.Value;
                bool violates = requested < min;
                comparisons.Add(new PolicyDeviationModel
                {
                    Field = "Margem",
                    PolicyValue = $"mín {FormatPercent(min)}",
                    RequestedValue = FormatPercent(requested),
                    Delta = violates ? $"-{FormatPercentPoints(min - requested)}" : "dentro",
                    Kind = violates ? 3 : 1,
                    IsViolation = violates,
                });

                if (violates)
                {
                    decimal lostMargin = negotiation.Amount * ((min - requested) / 100m);
                    impacts.Add(new PolicyImpactModel { Label = "Margem", Value = $"-{FormatBrl(lostMargin)}", IsGood = false });
                }
            }

            if (policy.MaxPaymentTermDays.HasValue && negotiation.PaymentTermDays.HasValue)
            {
                int requested = negotiation.PaymentTermDays.Value;
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
            else if (comparisons.Any(d => d.IsViolation && d.Field == "Margem"))
            {
                suggestedType = "margin";
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
