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

            List<PolicyDeviationModel> deviations = [];
            List<PolicyImpactModel> impacts = [];

            if (policy.MaxDiscountPercent.HasValue && negotiation.DiscountPercent.HasValue && negotiation.DiscountPercent.Value > policy.MaxDiscountPercent.Value)
            {
                decimal extraPp = negotiation.DiscountPercent.Value - policy.MaxDiscountPercent.Value;
                deviations.Add(new PolicyDeviationModel
                {
                    Field = "Desconto",
                    PolicyValue = FormatPercent(policy.MaxDiscountPercent.Value),
                    RequestedValue = FormatPercent(negotiation.DiscountPercent.Value),
                    Delta = $"+{FormatPercentPoints(extraPp)}",
                    Kind = 2,
                });

                decimal lostRevenue = negotiation.Amount * (extraPp / 100m);
                impacts.Add(new PolicyImpactModel
                {
                    Label = "Receita",
                    Value = $"-{FormatBrl(lostRevenue)}",
                    IsGood = false,
                });
            }

            if (policy.MinMarginPercent.HasValue && negotiation.MarginPercent.HasValue && negotiation.MarginPercent.Value < policy.MinMarginPercent.Value)
            {
                decimal lostPp = policy.MinMarginPercent.Value - negotiation.MarginPercent.Value;
                deviations.Add(new PolicyDeviationModel
                {
                    Field = "Margem",
                    PolicyValue = FormatPercent(policy.MinMarginPercent.Value),
                    RequestedValue = FormatPercent(negotiation.MarginPercent.Value),
                    Delta = $"-{FormatPercentPoints(lostPp)}",
                    Kind = 3,
                });

                decimal lostMargin = negotiation.Amount * (lostPp / 100m);
                impacts.Add(new PolicyImpactModel
                {
                    Label = "Margem",
                    Value = $"-{FormatBrl(lostMargin)}",
                    IsGood = false,
                });
            }

            if (policy.MaxPaymentTermDays.HasValue && negotiation.PaymentTermDays.HasValue && negotiation.PaymentTermDays.Value > policy.MaxPaymentTermDays.Value)
            {
                int extraDays = negotiation.PaymentTermDays.Value - policy.MaxPaymentTermDays.Value;
                int policyDays = policy.MaxPaymentTermDays.Value;
                deviations.Add(new PolicyDeviationModel
                {
                    Field = "Prazo de pagamento",
                    PolicyValue = $"{policyDays} dias",
                    RequestedValue = $"{negotiation.PaymentTermDays.Value} dias",
                    Delta = $"+{extraDays}d",
                    Kind = 2,
                });

                impacts.Add(new PolicyImpactModel
                {
                    Label = "Cashflow",
                    Value = $"-{extraDays} dias",
                    IsGood = false,
                });
            }

            string? suggestedType = null;
            if (deviations.Any(d => d.Field == "Desconto"))
            {
                suggestedType = "discount";
            }
            else if (deviations.Any(d => d.Field == "Margem"))
            {
                suggestedType = "margin";
            }
            else if (deviations.Any(d => d.Field == "Prazo de pagamento"))
            {
                suggestedType = "deadline";
            }
            else if (deviations.Count > 0)
            {
                suggestedType = "exception";
            }

            return new PolicyEvaluationModel
            {
                HasDeviations = deviations.Count > 0,
                PolicyMissing = false,
                SuggestedApprovalType = suggestedType,
                Deviations = deviations,
                Impacts = impacts,
            };
        }

        private static string FormatPercent(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') + "%";

        private static string FormatPercentPoints(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') + "pp";

        private static string FormatBrl(decimal value) => value.ToString("C0", new CultureInfo("pt-BR"));
    }
}
