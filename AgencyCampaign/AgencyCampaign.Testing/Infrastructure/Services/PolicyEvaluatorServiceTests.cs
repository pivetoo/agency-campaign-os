using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class PolicyEvaluatorServiceTests
    {
        private TestDbContext db = null!;
        private PolicyEvaluatorService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new PolicyEvaluatorService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Proposal> SeedProposalAsync(decimal? discount = null, int? paymentTermDays = null, decimal totalValue = 1000m)
        {
            decimal? discountAmount = discount.HasValue ? Math.Round(totalValue * discount.Value / 100m, 2) : null;
            Proposal proposal = new ProposalBuilder()
                .WithDiscountAmount(discountAmount)
                .WithPaymentTermDays(paymentTermDays)
                .WithTotalValue(totalValue)
                .Build();
            db.Add(proposal);
            await db.SaveChangesAsync();
            return proposal;
        }

        [Test]
        public async Task EvaluateProposalById_should_report_no_deviations_when_proposal_missing()
        {
            PolicyEvaluationModel result = await service.EvaluateProposalByIdAsync(99);

            result.HasDeviations.Should().BeFalse();
        }

        [Test]
        public async Task EvaluateProposal_should_report_policy_missing_when_no_policy_registered()
        {
            Proposal proposal = await SeedProposalAsync(discount: 50m);

            PolicyEvaluationModel result = await service.EvaluateProposalAsync(proposal);

            result.HasDeviations.Should().BeFalse();
            result.PolicyMissing.Should().BeTrue();
        }

        [Test]
        public async Task EvaluateProposal_should_flag_discount_violation()
        {
            db.Add(new CommercialPolicy(maxDiscountPercent: 10m, defaultPaymentTermDays: null, maxPaymentTermDays: null));
            Proposal proposal = await SeedProposalAsync(discount: 25m);

            PolicyEvaluationModel result = await service.EvaluateProposalAsync(proposal);

            result.HasDeviations.Should().BeTrue();
            result.SuggestedApprovalType.Should().Be("discount");
            result.Deviations.Should().Contain(item => item.Field == "Desconto" && item.IsViolation);
        }

        [Test]
        public async Task EvaluateProposal_should_flag_payment_term_violation()
        {
            db.Add(new CommercialPolicy(maxDiscountPercent: null, defaultPaymentTermDays: null, maxPaymentTermDays: 30));
            Proposal proposal = await SeedProposalAsync(paymentTermDays: 90);

            PolicyEvaluationModel result = await service.EvaluateProposalAsync(proposal);

            result.HasDeviations.Should().BeTrue();
            result.SuggestedApprovalType.Should().Be("deadline");
            result.Deviations.Should().Contain(item => item.Field == "Prazo de pagamento" && item.IsViolation);
        }

        [Test]
        public async Task EvaluateProposal_should_not_flag_when_within_policy()
        {
            db.Add(new CommercialPolicy(maxDiscountPercent: 30m, defaultPaymentTermDays: null, maxPaymentTermDays: 60));
            Proposal proposal = await SeedProposalAsync(discount: 10m, paymentTermDays: 30);

            PolicyEvaluationModel result = await service.EvaluateProposalAsync(proposal);

            result.HasDeviations.Should().BeFalse();
            result.Deviations.Should().OnlyContain(item => !item.IsViolation);
        }
    }
}
