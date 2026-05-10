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
            service = new CreatorService(db, LocalizerMock.Create<AgencyCampaignResource>());
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

            CampaignDeliverable overdue = new(11, 101, "2", 1, 1, DateTimeOffset.UtcNow.AddDays(-2), 100m, 80m, 10m);

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
