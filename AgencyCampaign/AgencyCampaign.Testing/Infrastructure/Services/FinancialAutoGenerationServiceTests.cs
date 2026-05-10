using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialAutoGenerationServiceTests
    {
        private TestDbContext db = null!;
        private FinancialAutoGenerationService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new FinancialAutoGenerationService(db);
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
        }

        private static FinancialAccount NewAccount(string name = "Conta principal")
        {
            return new FinancialAccount(name, FinancialAccountType.Bank, initialBalance: 0m, color: "#6366f1");
        }

        [Test]
        public async Task GenerateForConvertedProposal_should_be_no_op_when_entry_already_exists()
        {
            FinancialAccount account = NewAccount();
            db.Add(account);
            await db.SaveChangesAsync();

            Proposal proposal = new Proposal(opportunityId: 1, name: "P1", internalOwnerId: 1).WithId(10);
            FinancialEntry existing = new(
                accountId: account.Id,
                type: FinancialEntryType.Receivable,
                category: FinancialEntryCategory.BrandReceivable,
                description: "x",
                amount: 100m,
                dueAt: DateTimeOffset.UtcNow,
                occurredAt: DateTimeOffset.UtcNow);
            existing.LinkToProposal(proposal.Id);
            db.Add(existing);
            await db.SaveChangesAsync();

            await service.GenerateForConvertedProposal(proposal, campaignId: 1);

            int count = await db.Set<FinancialEntry>().CountAsync(item => item.SourceProposalId == proposal.Id);
            count.Should().Be(1);
        }

        [Test]
        public async Task GenerateForConvertedProposal_should_skip_when_no_active_account()
        {
            Proposal proposal = new Proposal(1, "P1", 1).WithId(10);
            proposal.UpdateTotalValue(500m);

            await service.GenerateForConvertedProposal(proposal, campaignId: 1);

            int count = await db.Set<FinancialEntry>().CountAsync();
            count.Should().Be(0);
        }

        [Test]
        public async Task GenerateForConvertedProposal_should_create_receivable_with_brand_name_and_validity_due_date()
        {
            FinancialAccount account = NewAccount();
            db.Add(account);

            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();

            Campaign campaign = new(brandId: brand.Id, name: "Camp", budget: 0, startsAt: DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();

            DateTimeOffset validity = DateTimeOffset.UtcNow.AddDays(7);
            Proposal proposal = new Proposal(opportunityId: 1, name: "Proposta X", internalOwnerId: 1, validityUntil: validity).WithId(10);
            proposal.UpdateTotalValue(2500m);

            await service.GenerateForConvertedProposal(proposal, campaignId: campaign.Id);

            FinancialEntry entry = await db.Set<FinancialEntry>().SingleAsync();
            entry.AccountId.Should().Be(account.Id);
            entry.Type.Should().Be(FinancialEntryType.Receivable);
            entry.Category.Should().Be(FinancialEntryCategory.BrandReceivable);
            entry.Amount.Should().Be(2500m);
            entry.CounterpartyName.Should().Be("Acme");
            entry.CampaignId.Should().Be(campaign.Id);
            entry.SourceProposalId.Should().Be(proposal.Id);
            entry.DueAt.Should().BeCloseTo(validity, TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task GenerateForConvertedProposal_should_use_default_due_date_when_validity_missing()
        {
            FinancialAccount account = NewAccount();
            db.Add(account);
            await db.SaveChangesAsync();

            Proposal proposal = new Proposal(1, "P", 1).WithId(11);
            proposal.UpdateTotalValue(100m);

            await service.GenerateForConvertedProposal(proposal, campaignId: 99);

            FinancialEntry entry = await db.Set<FinancialEntry>().SingleAsync();
            entry.DueAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
            entry.CounterpartyName.Should().Be("Marca");
        }

        [Test]
        public async Task GenerateForConvertedProposal_should_throw_on_null_proposal()
        {
            Func<Task> act = () => service.GenerateForConvertedProposal(null!, 1);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task GenerateForPublishedDeliverable_should_skip_when_creator_amount_is_zero()
        {
            CampaignDeliverable deliverable = new CampaignDeliverable(
                campaignId: 1, campaignCreatorId: 1, title: "x", deliverableKindId: 1, platformId: 1,
                dueAt: DateTimeOffset.UtcNow, grossAmount: 100m, creatorAmount: 0m, agencyFeeAmount: 0m).WithId(5);

            await service.GenerateForPublishedDeliverable(deliverable);

            (await db.Set<FinancialEntry>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task GenerateForPublishedDeliverable_should_skip_when_entry_already_exists()
        {
            FinancialAccount account = NewAccount("Conta");
            db.Add(account);
            await db.SaveChangesAsync();

            CampaignDeliverable deliverable = new CampaignDeliverable(
                campaignId: 1, campaignCreatorId: 1, title: "x", deliverableKindId: 1, platformId: 1,
                dueAt: DateTimeOffset.UtcNow, grossAmount: 1000m, creatorAmount: 800m, agencyFeeAmount: 100m).WithId(5);

            FinancialEntry existing = new(
                accountId: account.Id, type: FinancialEntryType.Payable, category: FinancialEntryCategory.CreatorPayout,
                description: "x", amount: 100m, dueAt: DateTimeOffset.UtcNow, occurredAt: DateTimeOffset.UtcNow,
                campaignDeliverableId: deliverable.Id);
            db.Add(existing);
            await db.SaveChangesAsync();

            await service.GenerateForPublishedDeliverable(deliverable);

            (await db.Set<FinancialEntry>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task GenerateForPublishedDeliverable_should_create_payable_with_creator_stage_name()
        {
            FinancialAccount account = NewAccount("Conta");
            db.Add(account);

            Creator creator = new("Foo Real", stageName: "Foo Stage");
            db.Add(creator);
            await db.SaveChangesAsync();

            CampaignCreator campaignCreator = new(campaignId: 1, creatorId: creator.Id, campaignCreatorStatusId: 1, agreedAmount: 1000m, agencyFeePercent: 10m);
            db.Add(campaignCreator);
            await db.SaveChangesAsync();

            DateTimeOffset publishedAt = DateTimeOffset.UtcNow.AddDays(-1);
            CampaignDeliverable deliverable = new CampaignDeliverable(
                campaignId: 1, campaignCreatorId: campaignCreator.Id, title: "Story", deliverableKindId: 1, platformId: 1,
                dueAt: DateTimeOffset.UtcNow, grossAmount: 1000m, creatorAmount: 800m, agencyFeeAmount: 100m).WithId(7);
            deliverable.Publish("https://x", null, publishedAt);

            await service.GenerateForPublishedDeliverable(deliverable);

            FinancialEntry entry = await db.Set<FinancialEntry>().SingleAsync();
            entry.Type.Should().Be(FinancialEntryType.Payable);
            entry.Category.Should().Be(FinancialEntryCategory.CreatorPayout);
            entry.Amount.Should().Be(800m);
            entry.CounterpartyName.Should().Be("Foo Stage");
            entry.CampaignDeliverableId.Should().Be(deliverable.Id);
            entry.DueAt.Should().BeCloseTo(publishedAt.AddDays(15), TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task GenerateForPublishedDeliverable_should_throw_on_null_deliverable()
        {
            Func<Task> act = () => service.GenerateForPublishedDeliverable(null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
