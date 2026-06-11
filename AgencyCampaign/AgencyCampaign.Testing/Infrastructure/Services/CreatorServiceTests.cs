using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Creators;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using DomainEntities = AgencyCampaign.Domain.Entities;
using CampaignCreatorStatus = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CreatorServiceTests
    {
        private TestDbContext db = null!;
        private CreatorService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CreatorService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreateCreator_should_persist_with_normalized_data()
        {
            CreateCreatorRequest request = new() { Name = " Foo ", StageName = " Bar " };

            Creator creator = await service.CreateCreator(request);

            creator.Name.Should().Be("Foo");
            creator.StageName.Should().Be("Bar");
            (await db.Set<Creator>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task CreateCreator_should_persist_tax_regime()
        {
            CreateCreatorRequest request = new() { Name = "Joana", TaxRegime = TaxRegime.SimplesNacional };

            Creator creator = await service.CreateCreator(request);

            creator.TaxRegime.Should().Be(TaxRegime.SimplesNacional);
        }

        [Test]
        public async Task UpdateCreator_should_throw_when_id_mismatch()
        {
            UpdateCreatorRequest request = new() { Id = 5, Name = "x" };

            Func<Task> act = () => service.UpdateCreator(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateCreator_should_throw_when_not_found()
        {
            UpdateCreatorRequest request = new() { Id = 1, Name = "x" };
            Func<Task> act = () => service.UpdateCreator(1, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetSummary_should_return_null_when_creator_not_found()
        {
            (await service.GetSummary(99)).Should().BeNull();
        }

        [Test]
        public async Task GetCreators_should_filter_inactive_by_default()
        {
            Creator active = new("Alice");
            Creator inactive = new("Bob");
            inactive.Update("Bob", null, null, null, null, null, null, null, null, null, null, 0m, isActive: false);
            db.Add(active);
            db.Add(inactive);
            await db.SaveChangesAsync();

            Archon.Core.Pagination.PagedResult<Creator> result = await service.GetCreators(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: false);

            result.Items.Should().HaveCount(1);
            result.Items.First().Name.Should().Be("Alice");
        }

        [Test]
        public async Task GetCreators_should_search_by_name_or_stage_name()
        {
            db.Add(new Creator("Foo Bar", stageName: "FooBarStage"));
            db.Add(new Creator("Outro", stageName: "BarStage"));
            await db.SaveChangesAsync();

            Archon.Core.Pagination.PagedResult<Creator> byName = await service.GetCreators(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, "foo", includeInactive: true);
            Archon.Core.Pagination.PagedResult<Creator> byStage = await service.GetCreators(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, "barstage", includeInactive: true);

            byName.Items.Should().ContainSingle();
            byStage.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task GetCreatorById_should_return_null_when_not_found()
        {
            (await service.GetCreatorById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetCreatorById_should_return_creator_when_found()
        {
            Creator creator = new("Alice");
            db.Add(creator);
            await db.SaveChangesAsync();

            Creator? result = await service.GetCreatorById(creator.Id);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Alice");
        }

        [Test]
        public async Task UpdateCreator_should_persist_changes()
        {
            Creator creator = new("Alice");
            db.Add(creator);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCreatorRequest request = new()
            {
                Id = creator.Id,
                Name = "Updated",
                Email = "new@x"
            };

            Creator result = await service.UpdateCreator(creator.Id, request);

            result.Name.Should().Be("Updated");
            result.Email.Should().Be("new@x");
        }

        [Test]
        public async Task SetCreatorPhoto_should_persist_photo_url()
        {
            Creator creator = new("Alice");
            db.Add(creator);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Creator result = await service.SetCreatorPhoto(creator.Id, "/photo.jpg");

            result.PhotoUrl.Should().Be("/photo.jpg");
        }

        [Test]
        public async Task SetCreatorPhoto_should_throw_when_not_found()
        {
            Func<Task> act = () => service.SetCreatorPhoto(99, "/photo.jpg");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task RemoveCreatorPhoto_should_clear_photo_url()
        {
            Creator creator = new("Alice");
            db.Add(creator);
            await db.SaveChangesAsync();
            await service.SetCreatorPhoto(creator.Id, "/photo.jpg");
            db.ChangeTracker.Clear();

            Creator result = await service.RemoveCreatorPhoto(creator.Id);

            result.PhotoUrl.Should().BeNull();
        }

        [Test]
        public async Task GetCampaignsByCreator_should_return_empty_when_none()
        {
            IReadOnlyCollection<DomainEntities.CampaignCreator> result = await service.GetCampaignsByCreator(99);

            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetCampaignsByCreator_should_return_associations()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "C1", 0m, DateTimeOffset.UtcNow).WithId(10));
            db.Add(new Creator("Foo").WithId(1));
            db.Add(new CampaignCreatorStatus("Open", 1, "#fff").WithId(50));
            db.Add(new DomainEntities.CampaignCreator(10, 1, 50, 100m, 10m).WithId(100));
            await db.SaveChangesAsync();

            IReadOnlyCollection<DomainEntities.CampaignCreator> result = await service.GetCampaignsByCreator(1);

            result.Should().HaveCount(1);
        }

        [Test]
        public async Task GetSummary_should_return_zero_metrics_for_creator_with_no_campaigns()
        {
            db.Add(new Creator("Solo").WithId(7));
            await db.SaveChangesAsync();

            CreatorSummaryModel? summary = await service.GetSummary(7);

            summary.Should().NotBeNull();
            summary!.CreatorId.Should().Be(7);
            summary.TotalCampaigns.Should().Be(0);
            summary.TotalDeliverables.Should().Be(0);
            summary.OnTimeDeliveryRate.Should().Be(0);
            summary.PerformanceByPlatform.Should().BeEmpty();
        }

        [Test]
        public async Task GetSummary_should_use_creator_name_when_stage_name_is_null()
        {
            Creator creator = new Creator("Pessoa Real").WithId(8);
            db.Add(creator);
            await db.SaveChangesAsync();

            CreatorSummaryModel? summary = await service.GetSummary(8);

            summary!.CreatorName.Should().Be("Pessoa Real");
        }

        [Test]
        public async Task GetSummary_should_use_stage_name_when_provided()
        {
            Creator creator = new Creator("Pessoa Real", stageName: "@nick").WithId(9);
            db.Add(creator);
            await db.SaveChangesAsync();

            CreatorSummaryModel? summary = await service.GetSummary(9);

            summary!.CreatorName.Should().Be("@nick");
        }

        [Test]
        public async Task GetSummary_should_group_by_multiple_platforms_in_performance()
        {
            db.Add(new Creator("X").WithId(15));
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "C", 0m, DateTimeOffset.UtcNow).WithId(20));
            db.Add(new Platform("Instagram").WithId(1));
            db.Add(new Platform("TikTok").WithId(2));
            db.Add(new DeliverableKind("Story").WithId(1));
            db.Add(new CampaignCreatorStatus("Open", 1, "#fff").WithId(50));
            db.Add(new DomainEntities.CampaignCreator(20, 15, 50, 100m, 10m).WithId(200));

            db.Add(new CampaignDeliverable(20, 200, "Ig1", 1, 1, DateTimeOffset.UtcNow.AddDays(1), 100m, 80m, 10m));
            db.Add(new CampaignDeliverable(20, 200, "Ig2", 1, 1, DateTimeOffset.UtcNow.AddDays(1), 200m, 160m, 20m));
            db.Add(new CampaignDeliverable(20, 200, "Tk1", 1, 2, DateTimeOffset.UtcNow.AddDays(1), 50m, 40m, 5m));
            await db.SaveChangesAsync();

            CreatorSummaryModel? summary = await service.GetSummary(15);

            summary!.PerformanceByPlatform.Should().HaveCount(2);
            summary.PerformanceByPlatform.First().GrossAmount.Should().BeGreaterThan(summary.PerformanceByPlatform.Last().GrossAmount);
        }

        [Test]
        public async Task GetSummary_should_compute_aggregated_metrics()
        {
            Creator creator = new Creator("Foo").WithId(1);
            db.Add(creator);
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "C1", 0m, DateTimeOffset.UtcNow).WithId(10));
            db.Add(new Campaign(1, "C2", 0m, DateTimeOffset.UtcNow).WithId(11));
            db.Add(new Platform("IG").WithId(1));
            db.Add(new DeliverableKind("Story").WithId(1));

            CampaignCreatorStatus confirmedStatus = new CampaignCreatorStatus("Confirmado", 1, "#fff", category: CampaignCreatorStatusCategory.Success, marksAsConfirmed: true).WithId(50);
            db.Add(confirmedStatus);

            CampaignCreator confirmed = new DomainEntities.CampaignCreator(10, 1, 50, 100m, 10m).WithId(100);
            confirmed.ChangeStatus(confirmedStatus);
            CampaignCreator open = new DomainEntities.CampaignCreator(11, 1, 50, 100m, 10m).WithId(101);
            db.Add(confirmed);
            db.Add(open);

            DateTimeOffset publishedAt = DateTimeOffset.UtcNow.AddDays(-1);
            CampaignDeliverable onTime = new(10, 100, "1", 1, 1, DateTimeOffset.UtcNow.AddDays(1), 100m, 80m, 10m);
            onTime.Publish("https://x", null, publishedAt);

            // Entregavel ja vencido: o construtor recusa prazo no passado, entao forcamos o DueAt via reflexao.
            CampaignDeliverable overdue = new(11, 101, "2", 1, 1, DateTimeOffset.UtcNow.AddDays(1), 100m, 80m, 10m);
            typeof(CampaignDeliverable).GetProperty(nameof(CampaignDeliverable.DueAt))!.SetValue(overdue, DateTimeOffset.UtcNow.AddDays(-2));

            db.Add(onTime);
            db.Add(overdue);
            await db.SaveChangesAsync();

            CreatorSummaryModel? summary = await service.GetSummary(1);

            summary.Should().NotBeNull();
            summary!.TotalCampaigns.Should().Be(2);
            summary.ConfirmedCampaigns.Should().Be(1);
            summary.TotalDeliverables.Should().Be(2);
            summary.PublishedDeliverables.Should().Be(1);
            summary.OverdueDeliverables.Should().Be(1);
            summary.OnTimeDeliveryRate.Should().Be(100m);
            summary.PerformanceByPlatform.Should().HaveCount(1);
        }
    }
}
