using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityNegotiationServiceTests
    {
        private TestDbContext db = null!;
        private OpportunityNegotiationService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new OpportunityNegotiationService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Opportunity> SeedOpportunityAsync()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            await db.SaveChangesAsync();
            Opportunity opportunity = new(brandId: 1, commercialPipelineStageId: 1, name: "x", estimatedValue: 0);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            return opportunity;
        }

        [Test]
        public async Task CreateOpportunityNegotiation_should_throw_when_opportunity_not_found()
        {
            CreateOpportunityNegotiationRequest request = new()
            {
                OpportunityId = 99,
                Title = "v1",
                Amount = 100m,
                NegotiatedAt = DateTimeOffset.UtcNow
            };

            Func<Task> act = () => service.CreateOpportunityNegotiation(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunityNegotiation_should_persist_with_default_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            OpportunityNegotiation negotiation = await service.CreateOpportunityNegotiation(new CreateOpportunityNegotiationRequest
            {
                OpportunityId = opportunity.Id,
                Title = "v1",
                Amount = 1000m,
                NegotiatedAt = DateTimeOffset.UtcNow
            });

            negotiation.Status.Should().Be(OpportunityNegotiationStatus.Draft);
            (await db.Set<OpportunityNegotiation>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task ChangeStatus_should_apply_state_machine_transition()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityNegotiation negotiation = new(opportunity.Id, "v1", 100m, DateTimeOffset.UtcNow);
            db.Add(negotiation);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityNegotiation result = await service.ChangeStatus(negotiation.Id, new ChangeOpportunityNegotiationStatusRequest
            {
                Status = OpportunityNegotiationStatus.Approved
            });

            result.Status.Should().Be(OpportunityNegotiationStatus.Approved);
        }

        [Test]
        public async Task ChangeStatus_should_propagate_invalid_state_transitions()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityNegotiation negotiation = new(opportunity.Id, "v1", 100m, DateTimeOffset.UtcNow);
            db.Add(negotiation);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.ChangeStatus(negotiation.Id, new ChangeOpportunityNegotiationStatusRequest
            {
                Status = OpportunityNegotiationStatus.SentToClient
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ChangeStatus_should_throw_when_negotiation_not_found()
        {
            Func<Task> act = () => service.ChangeStatus(99, new ChangeOpportunityNegotiationStatusRequest
            {
                Status = OpportunityNegotiationStatus.Approved
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task DeleteOpportunityNegotiation_should_throw_when_not_found()
        {
            Func<Task> act = () => service.DeleteOpportunityNegotiation(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task DeleteOpportunityNegotiation_should_remove_record()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityNegotiation negotiation = new(opportunity.Id, "v1", 100m, DateTimeOffset.UtcNow);
            db.Add(negotiation);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await service.DeleteOpportunityNegotiation(negotiation.Id);

            (await db.Set<OpportunityNegotiation>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task GetNegotiationsByOpportunityId_should_filter_and_order_by_negotiated_at_desc()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            DateTimeOffset baseDate = DateTimeOffset.UtcNow;

            db.Add(new OpportunityNegotiation(opportunity.Id, "v1", 100m, baseDate.AddDays(-2)).WithId(1));
            db.Add(new OpportunityNegotiation(opportunity.Id, "v2", 200m, baseDate.AddDays(-1)).WithId(2));
            db.Add(new OpportunityNegotiation(opportunity.Id, "v3", 300m, baseDate).WithId(3));
            db.Add(new OpportunityNegotiation(99, "outra", 1m, baseDate).WithId(4));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityNegotiation> result = await service.GetNegotiationsByOpportunityId(opportunity.Id);

            result.Select(item => item.Title).Should().Equal("v3", "v2", "v1");
        }
    }
}
