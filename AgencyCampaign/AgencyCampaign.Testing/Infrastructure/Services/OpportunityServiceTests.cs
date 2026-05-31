using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Exceptions;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityServiceTests
    {
        private TestDbContext db = null!;
        private Mock<Archon.Application.Services.INotificationService> notifications = null!;
        private OpportunityService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new();
            service = new OpportunityService(db, CurrentUserMock.Create(), IdentityClientFactory.CreateInert(), notifications.Object);
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

        [Test]
        public async Task GetOpportunityById_should_return_null_when_not_found()
        {
            Opportunity? result = await service.GetOpportunityById(99);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetOpportunityById_should_return_with_includes_when_found()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "Big", 100m);
            db.Add(opportunity);
            await db.SaveChangesAsync();

            Opportunity? result = await service.GetOpportunityById(opportunity.Id);

            result.Should().NotBeNull();
            result!.Brand.Should().NotBeNull();
            result.CommercialPipelineStage.Should().NotBeNull();
        }

        [Test]
        public async Task CreateOpportunity_should_throw_when_stage_id_provided_but_not_found()
        {
            await SeedBaseAsync();

            Func<Task> act = () => service.CreateOpportunity(new CreateOpportunityRequest
            {
                BrandId = 1,
                CommercialPipelineStageId = 999,
                Name = "x",
                EstimatedValue = 0
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateOpportunity_should_apply_tags_and_source_when_provided()
        {
            await SeedBaseAsync();
            db.Add(new OpportunitySource("Indicação", "#fff", 1).WithId(50));
            db.Add(new OpportunityTag("vip", "#000").WithId(60));
            db.Add(new OpportunityTag("hot", "#111").WithId(61));
            await db.SaveChangesAsync();

            Opportunity result = await service.CreateOpportunity(new CreateOpportunityRequest
            {
                BrandId = 1,
                Name = "Big",
                EstimatedValue = 100m,
                OpportunitySourceId = 50,
                TagIds = new List<long> { 60, 61 }
            });

            result.OpportunitySourceId.Should().Be(50);
            result.TagAssignments.Should().HaveCount(2);
        }

        [Test]
        public async Task UpdateOpportunity_should_throw_when_route_id_does_not_match_body()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "x", 0m);
            db.Add(opportunity);
            await db.SaveChangesAsync();

            UpdateOpportunityRequest request = new() { Id = 999, BrandId = 1, Name = "x", EstimatedValue = 0 };

            Func<Task> act = () => service.UpdateOpportunity(opportunity.Id, request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("request.route.idMismatch");
        }

        [Test]
        public async Task UpdateOpportunity_should_throw_when_not_found()
        {
            await SeedBaseAsync();

            UpdateOpportunityRequest request = new() { Id = 99, BrandId = 1, Name = "x", EstimatedValue = 0 };

            Func<Task> act = () => service.UpdateOpportunity(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task UpdateOpportunity_should_persist_changes_and_replace_tags()
        {
            await SeedBaseAsync();
            db.Add(new OpportunityTag("a", "#fff").WithId(60));
            db.Add(new OpportunityTag("b", "#000").WithId(61));
            db.Add(new OpportunityTag("c", "#111").WithId(62));
            Opportunity opportunity = new(1, 1, "Old", 100m);
            opportunity.ReplaceTags(new long[] { 60, 61 });
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateOpportunityRequest request = new()
            {
                Id = opportunity.Id,
                BrandId = 1,
                Name = "Updated",
                EstimatedValue = 250m,
                TagIds = new List<long> { 61, 62 }
            };

            Opportunity result = await service.UpdateOpportunity(opportunity.Id, request);

            result.Name.Should().Be("Updated");
            result.EstimatedValue.Should().Be(250m);
            result.TagAssignments.Select(item => item.OpportunityTagId).Should().BeEquivalentTo(new long[] { 61, 62 });
        }

        [Test]
        public async Task CloseAsLost_should_throw_when_no_lost_stage_configured()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            Opportunity opportunity = new(1, 1, "Big", 1000m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.CloseAsLost(opportunity.Id, new CloseOpportunityAsLostRequest { LossReason = "preço" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ChangeStage_should_throw_when_opportunity_not_found()
        {
            await SeedBaseAsync();

            Func<Task> act = () => service.ChangeStage(99, new ChangeOpportunityStageRequest { CommercialPipelineStageId = 2 });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetBoard_should_return_stages_with_opportunities_grouped()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "a", 100m));
            db.Add(new Opportunity(1, 2, "b", 200m));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityBoardStageModel> result = await service.GetBoard();

            result.Should().HaveCount(4);
            result.First(stage => stage.CommercialPipelineStageId == 1).Items.Should().HaveCount(1);
            result.First(stage => stage.CommercialPipelineStageId == 2).Items.Should().HaveCount(1);
            result.First(stage => stage.CommercialPipelineStageId == 2).EstimatedValueTotal.Should().Be(200m);
        }

        [Test]
        public async Task GetAlerts_should_emit_followup_alerts()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "x", 0m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            db.Add(new OpportunityFollowUp(opportunity.Id, "Ligar", DateTimeOffset.UtcNow.AddDays(-1), "x"));
            await db.SaveChangesAsync();

            IReadOnlyCollection<CommercialAlertModel> result = await service.GetAlerts();

            result.Should().Contain(item => item.Type == "followup");
        }

        [Test]
        public async Task GetAlerts_should_emit_expected_close_overdue_alerts()
        {
            await SeedBaseAsync();
            Opportunity opportunity = new(1, 1, "x", 0m, expectedCloseAt: DateTimeOffset.UtcNow.AddDays(-2));
            db.Add(opportunity);
            await db.SaveChangesAsync();

            IReadOnlyCollection<CommercialAlertModel> result = await service.GetAlerts();

            result.Should().Contain(item => item.Type == "expectedclose");
        }

        [Test]
        public async Task AlertStalled_should_notify_open_opportunities_past_stage_sla_once()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new CommercialPipelineStageBuilder().WithId(1).WithSlaInDays(3).Build());
            await db.SaveChangesAsync();

            Opportunity stalled = new(1, 1, "Parada", 0m, responsibleUserId: 7, responsibleUserName: "Owner");
            Opportunity fresh = new(1, 1, "Recente", 0m);
            db.Add(stalled);
            db.Add(fresh);
            await db.SaveChangesAsync();

            // Sem stage history -> AlertStalled cai no fallback CreatedAt; backdata a parada alem do SLA.
            db.Set<OpportunityStageHistory>().RemoveRange(db.Set<OpportunityStageHistory>());
            stalled.SetCreatedAt(DateTimeOffset.UtcNow.AddDays(-10));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            int count = await service.AlertStalled();

            count.Should().Be(1);
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            db.ChangeTracker.Clear();
            (await db.Set<Opportunity>().AsNoTracking().SingleAsync(item => item.Id == stalled.Id)).StaleAlertedAt.Should().NotBeNull();

            (await service.AlertStalled()).Should().Be(0);
        }

        [Test]
        public async Task GetOpportunities_should_apply_responsible_filter()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "mine", 100m, responsibleUserId: 7, responsibleUserName: "Alice"));
            db.Add(new Opportunity(1, 1, "other", 100m, responsibleUserId: 99, responsibleUserName: "Bob"));
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { ResponsibleUserId = 7 });

            result.Items.Should().ContainSingle(item => item.Name == "mine");
        }

        [Test]
        public async Task GetOpportunities_should_apply_search_filter_by_name()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Hello world", 100m));
            db.Add(new Opportunity(1, 1, "Outro nome", 100m));
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { Search = "hello" });

            result.Items.Should().ContainSingle(item => item.Name == "Hello world");
        }

        [Test]
        public async Task GetOpportunities_should_apply_status_filter_for_lost()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "open", 100m));
            db.Add(new Opportunity(1, 4, "lost", 100m));
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { Status = "lost" });

            result.Items.Should().ContainSingle(item => item.Name == "lost");
        }

        [Test]
        public async Task GetOpportunities_should_apply_status_filter_for_open()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "open", 100m));
            db.Add(new Opportunity(1, 3, "won", 100m));
            db.Add(new Opportunity(1, 4, "lost", 100m));
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { Status = "open" });

            result.Items.Should().ContainSingle(item => item.Name == "open");
        }

        [Test]
        public async Task GetOpportunities_should_apply_stage_filter()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "stage-1", 100m));
            db.Add(new Opportunity(1, 2, "stage-2", 100m));
            await db.SaveChangesAsync();

            PagedResult<Opportunity> result = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { CommercialPipelineStageId = 2 });

            result.Items.Should().ContainSingle(item => item.Name == "stage-2");
        }

        [Test]
        public async Task GetOpportunities_should_apply_source_and_tag_filters()
        {
            await SeedBaseAsync();
            db.Add(new OpportunitySource("ind", "#fff", 1).WithId(50));
            db.Add(new OpportunityTag("vip", "#fff").WithId(60));

            Opportunity withSourceAndTag = new(1, 1, "a", 100m);
            withSourceAndTag.SetSource(50);
            withSourceAndTag.ReplaceTags(new long[] { 60 });
            db.Add(withSourceAndTag);

            Opportunity other = new(1, 1, "b", 100m);
            db.Add(other);
            await db.SaveChangesAsync();

            PagedResult<Opportunity> bySource = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { OpportunitySourceId = 50 });
            PagedResult<Opportunity> byTag = await service.GetOpportunities(new PagedRequest { Page = 1, PageSize = 10 }, new OpportunityListFilters { OpportunityTagId = 60 });

            bySource.Items.Should().ContainSingle(item => item.Name == "a");
            byTag.Items.Should().ContainSingle(item => item.Name == "a");
        }

        [Test]
        public async Task GetOpportunityById_restricted_should_return_null_when_not_owner()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Alheia", 500m, responsibleUserId: 2).WithId(10));
            await db.SaveChangesAsync();

            Opportunity? result = await service.GetOpportunityById(10, restrictToCurrentUser: true);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetOpportunityById_restricted_should_return_opportunity_when_owner()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Minha", 500m, responsibleUserId: 1).WithId(11));
            await db.SaveChangesAsync();

            Opportunity? result = await service.GetOpportunityById(11, restrictToCurrentUser: true);

            result.Should().NotBeNull();
            result!.Id.Should().Be(11);
        }

        [Test]
        public async Task ChangeStage_restricted_should_throw_NotFound_when_not_owner()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Alheia", 500m, responsibleUserId: 2).WithId(12));
            await db.SaveChangesAsync();

            Func<Task> act = () => service.ChangeStage(12, new ChangeOpportunityStageRequest { CommercialPipelineStageId = 2 }, restrictToCurrentUser: true);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Test]
        public async Task Delete_restricted_should_keep_record_when_not_owner()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Alheia", 500m, responsibleUserId: 2).WithId(13));
            await db.SaveChangesAsync();

            Opportunity? result = await service.Delete(13, restrictToCurrentUser: true);

            result.Should().BeNull();
            (await db.Set<Opportunity>().FindAsync(13L)).Should().NotBeNull();
        }

        [Test]
        public async Task GetForecast_should_count_open_opportunities_without_expected_date()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Sem data", 700m).WithId(20));
            db.Add(new Opportunity(1, 1, "Com data", 300m, expectedCloseAt: new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero)).WithId(21));
            await db.SaveChangesAsync();

            CommercialForecastModel forecast = await service.GetForecast(
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
                restrictToCurrentUser: false,
                userId: null);

            forecast.NoDateCount.Should().Be(1);
            forecast.NoDateTotal.Should().Be(700m);
        }

        [Test]
        public async Task ChangeStage_should_throw_conflict_when_expected_version_is_stale()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Deal", 100m, responsibleUserId: 1).WithId(30));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.ChangeStage(30, new ChangeOpportunityStageRequest { CommercialPipelineStageId = 2, ExpectedVersion = 5 });

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Test]
        public async Task ChangeStage_should_increment_version_on_success()
        {
            await SeedBaseAsync();
            db.Add(new Opportunity(1, 1, "Deal", 100m, responsibleUserId: 1).WithId(31));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await service.ChangeStage(31, new ChangeOpportunityStageRequest { CommercialPipelineStageId = 2, ExpectedVersion = 0 });

            Opportunity? reloaded = await service.GetOpportunityById(31);
            reloaded!.Version.Should().Be(1);
        }
    }
}
