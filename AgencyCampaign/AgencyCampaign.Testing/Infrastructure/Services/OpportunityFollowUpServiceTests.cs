using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityFollowUpServiceTests
    {
        private TestDbContext db = null!;
        private OpportunityFollowUpService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new OpportunityFollowUpService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Opportunity> SeedOpportunityAsync()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            await db.SaveChangesAsync();
            Opportunity opportunity = new(1, 1, "x", 0);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            return opportunity;
        }

        [Test]
        public async Task CreateOpportunityFollowUp_should_throw_when_opportunity_not_found()
        {
            CreateOpportunityFollowUpRequest request = new()
            {
                OpportunityId = 99,
                Subject = "call",
                DueAt = DateTimeOffset.UtcNow
            };

            Func<Task> act = () => service.CreateOpportunityFollowUp(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunityFollowUp_should_persist()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            OpportunityFollowUp followUp = await service.CreateOpportunityFollowUp(new CreateOpportunityFollowUpRequest
            {
                OpportunityId = opportunity.Id,
                Subject = "call",
                DueAt = DateTimeOffset.UtcNow
            });

            followUp.Id.Should().BeGreaterThan(0);
            (await db.Set<OpportunityFollowUp>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task CompleteOpportunityFollowUp_should_mark_as_completed()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "call", DateTimeOffset.UtcNow);
            db.Add(followUp);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityFollowUp result = await service.CompleteOpportunityFollowUp(followUp.Id);

            result.IsCompleted.Should().BeTrue();
        }

        [Test]
        public async Task CompleteOpportunityFollowUp_should_throw_when_already_completed()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "call", DateTimeOffset.UtcNow);
            followUp.Complete();
            db.Add(followUp);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.CompleteOpportunityFollowUp(followUp.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CompleteOpportunityFollowUp_should_throw_when_not_found()
        {
            Func<Task> act = () => service.CompleteOpportunityFollowUp(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetFollowUpsByOpportunityId_should_order_open_first_then_by_due_date()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            DateTimeOffset baseDate = DateTimeOffset.UtcNow;

            OpportunityFollowUp openFar = new(opportunity.Id, "open-far", baseDate.AddDays(10));
            OpportunityFollowUp openSoon = new(opportunity.Id, "open-soon", baseDate.AddDays(1));
            OpportunityFollowUp closed = new(opportunity.Id, "closed", baseDate.AddDays(-1));
            closed.Complete();

            db.Add(openFar);
            db.Add(openSoon);
            db.Add(closed);
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityFollowUp> result = await service.GetFollowUpsByOpportunityId(opportunity.Id);

            result.Select(item => item.Subject).Should().Equal("open-soon", "open-far", "closed");
        }
    }
}
