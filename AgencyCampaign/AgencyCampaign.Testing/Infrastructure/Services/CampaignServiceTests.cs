using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Application.Requests.Campaigns;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using DomainEntities = AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignServiceTests
    {
        private TestDbContext db = null!;
        private CampaignService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CampaignService(db, LocalizerMock.Create<AgencyCampaignResource>(), CurrentUserMock.Create(), IdentityClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreateCampaign_should_throw_when_brand_not_found()
        {
            CreateCampaignRequest request = new()
            {
                BrandId = 99,
                Name = "x",
                StartsAt = DateTimeOffset.UtcNow
            };

            Func<Task> act = () => service.CreateCampaign(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateCampaign_should_persist_and_record_initial_status_history()
        {
            db.Add(new Brand("Acme").WithId(1));
            await db.SaveChangesAsync();

            Campaign campaign = await service.CreateCampaign(new CreateCampaignRequest
            {
                BrandId = 1,
                Name = "Camp",
                StartsAt = DateTimeOffset.UtcNow,
                Status = CampaignStatus.Draft
            });

            campaign.Id.Should().BeGreaterThan(0);

            CampaignStatusHistory history = await db.Set<CampaignStatusHistory>().AsNoTracking().SingleAsync();
            history.CampaignId.Should().Be(campaign.Id);
            history.FromStatus.Should().BeNull();
            history.ToStatus.Should().Be(CampaignStatus.Draft);
        }

        [Test]
        public async Task UpdateCampaign_should_throw_when_id_mismatch()
        {
            UpdateCampaignRequest request = new() { Id = 5, BrandId = 1, Name = "x", StartsAt = DateTimeOffset.UtcNow };

            Func<Task> act = () => service.UpdateCampaign(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateCampaign_should_throw_when_not_found()
        {
            UpdateCampaignRequest request = new() { Id = 99, BrandId = 1, Name = "x", StartsAt = DateTimeOffset.UtcNow };
            Func<Task> act = () => service.UpdateCampaign(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateCampaign_should_record_status_change_in_history()
        {
            db.Add(new Brand("Acme").WithId(1));
            Campaign campaign = new(1, "Camp", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignRequest request = new()
            {
                Id = campaign.Id,
                BrandId = 1,
                Name = "Camp",
                StartsAt = DateTimeOffset.UtcNow,
                Status = CampaignStatus.InProgress,
                IsActive = true
            };

            Campaign result = await service.UpdateCampaign(campaign.Id, request);

            result.Status.Should().Be(CampaignStatus.InProgress);

            List<CampaignStatusHistory> history = await db.Set<CampaignStatusHistory>().AsNoTracking().ToListAsync();
            history.Should().ContainSingle(item => item.FromStatus == CampaignStatus.Draft && item.ToStatus == CampaignStatus.InProgress);
        }

        [Test]
        public async Task UpdateCampaign_to_same_status_should_not_add_history()
        {
            db.Add(new Brand("Acme").WithId(1));
            Campaign campaign = new(1, "Camp", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignRequest request = new()
            {
                Id = campaign.Id,
                BrandId = 1,
                Name = "Camp",
                StartsAt = DateTimeOffset.UtcNow,
                Status = CampaignStatus.Draft,
                IsActive = true
            };

            await service.UpdateCampaign(campaign.Id, request);

            (await db.Set<CampaignStatusHistory>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task GetSummary_should_return_null_when_campaign_not_found()
        {
            (await service.GetSummary(99)).Should().BeNull();
        }

        [Test]
        public async Task GetSummary_should_aggregate_creators_deliverables_and_amounts()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "Camp", 5000m, DateTimeOffset.UtcNow).WithId(10));
            db.Add(new DomainEntities.CampaignCreator(10, 1, 1, 100m, 10m).WithId(20));
            db.Add(new DomainEntities.CampaignCreator(10, 2, 1, 100m, 10m).WithId(21));

            CampaignDeliverable d1 = new(10, 20, "p1", 1, 1, DateTimeOffset.UtcNow.AddDays(2), 1000m, 800m, 100m);
            CampaignDeliverable d2 = new(10, 21, "p2", 1, 1, DateTimeOffset.UtcNow.AddDays(2), 500m, 400m, 50m);
            d2.Publish("https://x", null, DateTimeOffset.UtcNow);
            db.Add(d1);
            db.Add(d2);
            await db.SaveChangesAsync();

            CampaignSummaryModel? summary = await service.GetSummary(10);

            summary.Should().NotBeNull();
            summary!.CampaignCreatorsCount.Should().Be(2);
            summary.DeliverablesCount.Should().Be(2);
            summary.PublishedDeliverablesCount.Should().Be(1);
            summary.PendingDeliverablesCount.Should().Be(1);
            summary.GrossAmountTotal.Should().Be(1500m);
            summary.RemainingBudget.Should().Be(3500m);
            summary.BrandName.Should().Be("Acme");
        }

        [Test]
        public async Task GetCampaigns_should_order_active_first_then_id_desc()
        {
            db.Add(new Brand("Acme").WithId(1));

            Campaign active1 = new(1, "Active1", 0m, DateTimeOffset.UtcNow);
            Campaign inactive = new(1, "Inactive", 0m, DateTimeOffset.UtcNow);
            inactive.Update(1, "Inactive", 0m, DateTimeOffset.UtcNow, null, null, null, null, CampaignStatus.Cancelled, null, null, false);
            Campaign active2 = new(1, "Active2", 0m, DateTimeOffset.UtcNow);

            db.Add(active1);
            db.Add(inactive);
            db.Add(active2);
            await db.SaveChangesAsync();

            var result = await service.GetCampaigns(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, null, true);

            result.Items.Last().IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetStatusHistory_should_filter_by_campaign_and_order_by_changed_at_desc()
        {
            db.Add(new CampaignStatusHistory(1, null, CampaignStatus.Draft, null, null).WithId(1));
            await Task.Delay(1);
            db.Add(new CampaignStatusHistory(1, CampaignStatus.Draft, CampaignStatus.InProgress, null, null).WithId(2));
            db.Add(new CampaignStatusHistory(2, null, CampaignStatus.Draft, null, null).WithId(3));
            await db.SaveChangesAsync();

            IReadOnlyCollection<CampaignStatusHistory> result = await service.GetStatusHistory(1);

            result.Select(item => item.Id).Should().BeEquivalentTo(new long[] { 1, 2 });
        }
    }
}
