using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityServiceTests
    {
        private TestDbContext db = null!;
        private OpportunityService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new OpportunityService(db, LocalizerMock.Create<AgencyCampaignResource>(), CurrentUserMock.Create(), IdentityClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task SeedBaseAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new CommercialPipelineStageBuilder().WithId(1).AsInitial().WithName("Qualificação").Build());
            db.Add(new CommercialPipelineStageBuilder().WithId(2).WithName("Proposta").WithDisplayOrder(2).Build());
            db.Add(new CommercialPipelineStageBuilder().WithId(3).WithName("Ganha").WithDisplayOrder(3).AsFinal(CommercialPipelineStageFinalBehavior.Won).Build());
            db.Add(new CommercialPipelineStageBuilder().WithId(4).WithName("Perdida").WithDisplayOrder(4).AsFinal(CommercialPipelineStageFinalBehavior.Lost).Build());
            await db.SaveChangesAsync();
        }

        [Test]
        public async Task CreateOpportunity_should_throw_when_brand_not_found()
        {
            CreateOpportunityRequest request = new() { BrandId = 99, Name = "x", EstimatedValue = 0 };
            Func<Task> act = () => service.CreateOpportunity(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunity_should_use_initial_stage_when_not_provided()
        {
            await SeedBaseAsync();

            Opportunity opportunity = await service.CreateOpportunity(new CreateOpportunityRequest
            {
                BrandId = 1,
                Name = "Big deal",
                EstimatedValue = 1000m
            });

            opportunity.CommercialPipelineStageId.Should().Be(1);
        }

        [Test]
        public async Task CreateOpportunity_should_throw_when_no_active_stages_configured()
        {
            db.Add(new Brand("Acme").WithId(1));
            await db.SaveChangesAsync();

            Func<Task> act = () => service.CreateOpportunity(new CreateOpportunityRequest
            {
                BrandId = 1, Name = "x", EstimatedValue = 0
            });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CloseAsWon_should_resolve_won_stage_and_close_opportunity()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "Big", 1000m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Opportunity result = await service.CloseAsWon(opportunity.Id, new CloseOpportunityAsWonRequest { WonNotes = "fechou" });

            result.ClosedAt.Should().NotBeNull();
            result.WonNotes.Should().Be("fechou");
            result.CommercialPipelineStageId.Should().Be(3);
        }

        [Test]
        public async Task CloseAsLost_should_require_loss_reason()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "Big", 1000m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Opportunity result = await service.CloseAsLost(opportunity.Id, new CloseOpportunityAsLostRequest { LossReason = "preço" });

            result.LossReason.Should().Be("preço");
            result.CommercialPipelineStageId.Should().Be(4);
        }

        [Test]
        public async Task CloseAsWon_should_throw_when_no_won_stage_configured()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            Opportunity opportunity = new(1, 1, "Big", 1000m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.CloseAsWon(opportunity.Id, new CloseOpportunityAsWonRequest());
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ChangeStage_should_promote_opportunity_to_target_stage()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "Big", 1000m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Opportunity result = await service.ChangeStage(opportunity.Id, new ChangeOpportunityStageRequest { CommercialPipelineStageId = 2, Reason = "moveu" });

            result.CommercialPipelineStageId.Should().Be(2);
        }

        [Test]
        public async Task ChangeStage_should_throw_when_target_stage_does_not_exist()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "Big", 1000m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.ChangeStage(opportunity.Id, new ChangeOpportunityStageRequest { CommercialPipelineStageId = 99 });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetOpportunities_should_apply_brand_filter()
        {
            await SeedBaseAsync();
            db.Add(new Brand("Other").WithId(2));
            db.Add(new Opportunity(1, 1, "Acme deal", 500m).WithId(10));
            db.Add(new Opportunity(2, 1, "Other deal", 500m).WithId(11));
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { BrandId = 1 });

            result.Items.Should().ContainSingle(item => item.Name == "Acme deal");
        }

        [Test]
        public async Task GetOpportunities_should_apply_value_range_filter()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "small", 100m).WithId(10));
            db.Add(new Opportunity(1, 1, "big", 5000m).WithId(11));
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 },
                new OpportunityListFilters { MinValue = 1000m, MaxValue = 10000m });

            result.Items.Should().ContainSingle(item => item.Name == "big");
        }

        [Test]
        public async Task GetOpportunities_should_apply_status_filter_for_won()
        {
            await SeedBaseAsync();
            Opportunity open = new(1, 1, "open", 100m);
            Opportunity won = new(1, 3, "won", 100m);
            db.Add(open);
            db.Add(won);
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 },
                new OpportunityListFilters { Status = "won" });

            result.Items.Should().ContainSingle(item => item.Name == "won");
        }

        [Test]
        public async Task GetDashboardSummary_should_aggregate_open_won_lost_and_value()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "open", 1000m));
            db.Add(new Opportunity(1, 3, "won", 5000m));
            db.Add(new Opportunity(1, 4, "lost", 2000m));
            await db.SaveChangesAsync();

            CommercialDashboardSummaryModel summary = await service.GetDashboardSummary();

            summary.TotalOpportunities.Should().Be(3);
            summary.OpenOpportunities.Should().Be(1);
            summary.WonOpportunities.Should().Be(1);
            summary.LostOpportunities.Should().Be(1);
            summary.TotalPipelineValue.Should().Be(1000m);
            summary.WonValue.Should().Be(5000m);
        }

        [Test]
        public async Task GetStageHistory_should_throw_when_opportunity_not_found()
        {
            Func<Task> act = () => service.GetStageHistory(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetStageHistory_should_return_history_ordered_desc()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "x", 0m);
            db.Add(opportunity);
            await db.SaveChangesAsync();

            db.Add(new OpportunityStageHistory(opportunity.Id, 1, 2, null, null, "moveu").WithId(50));
            await Task.Delay(2);
            db.Add(new OpportunityStageHistory(opportunity.Id, 2, 3, null, null, "ganha").WithId(51));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityStageHistoryModel> result = await service.GetStageHistory(opportunity.Id);

            result.Should().HaveCount(3); // 1 from constructor + 2 added manually
        }
    }
}
