using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignCreators;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using DomainEntities = AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignCreatorServiceTests
    {
        private TestDbContext db = null!;
        private Mock<INotificationService> notifications = null!;
        private CampaignCreatorService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new Mock<INotificationService>();
            service = new CampaignCreatorService(db, LocalizerMock.Create<AgencyCampaignResource>(), CurrentUserMock.Create(), notifications.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task SeedAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "C", 0m, DateTimeOffset.UtcNow).WithId(10));
            db.Add(new Creator("Foo").WithId(20));
            db.Add(new CampaignCreatorStatusBuilder().WithId(30).AsInitial().Build());
            db.Add(new CampaignCreatorStatusBuilder().WithId(31).WithCategory(CampaignCreatorStatusCategory.Success).MarksAsConfirmed().Build());
            await db.SaveChangesAsync();
        }

        [Test]
        public async Task CreateCampaignCreator_should_throw_when_campaign_not_found()
        {
            db.Add(new Creator("Foo").WithId(20));
            await db.SaveChangesAsync();

            CreateCampaignCreatorRequest request = new() { CampaignId = 99, CreatorId = 20, CampaignCreatorStatusId = 1 };

            Func<Task> act = () => service.CreateCampaignCreator(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateCampaignCreator_should_reject_duplicate_creator_per_campaign()
        {
            await SeedAsync();
            db.Add(new DomainEntities.CampaignCreator(10, 20, 30, 100m, 10m));
            await db.SaveChangesAsync();

            CreateCampaignCreatorRequest request = new() { CampaignId = 10, CreatorId = 20, CampaignCreatorStatusId = 30 };

            Func<Task> act = () => service.CreateCampaignCreator(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateCampaignCreator_should_use_initial_status_when_not_provided()
        {
            await SeedAsync();

            DomainEntities.CampaignCreator result = await service.CreateCampaignCreator(new CreateCampaignCreatorRequest
            {
                CampaignId = 10,
                CreatorId = 20,
                CampaignCreatorStatusId = 0,
                AgreedAmount = 100m,
                AgencyFeePercent = 0
            });

            result.CampaignCreatorStatusId.Should().Be(30);
            (await db.Set<CampaignCreatorStatusHistory>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task CreateCampaignCreator_should_use_creator_default_fee_when_request_fee_is_zero()
        {
            await SeedAsync();
            Creator updated = await db.Set<Creator>().AsTracking().FirstAsync(c => c.Id == 20);
            updated.Update(updated.Name, updated.StageName, updated.Email, updated.Phone, updated.Document, updated.PixKey, updated.PixKeyType, updated.PrimaryNiche, updated.City, updated.State, updated.Notes, defaultAgencyFeePercent: 25m, isActive: true);
            await db.SaveChangesAsync();

            DomainEntities.CampaignCreator result = await service.CreateCampaignCreator(new CreateCampaignCreatorRequest
            {
                CampaignId = 10,
                CreatorId = 20,
                CampaignCreatorStatusId = 30,
                AgreedAmount = 100m,
                AgencyFeePercent = 0
            });

            result.AgencyFeePercent.Should().Be(25m);
        }

        [Test]
        public async Task UpdateCampaignCreator_should_throw_when_id_mismatch()
        {
            UpdateCampaignCreatorRequest request = new() { Id = 5, CampaignCreatorStatusId = 1 };
            Func<Task> act = () => service.UpdateCampaignCreator(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateCampaignCreator_should_record_status_history_when_status_changes_and_notify_for_confirmed()
        {
            await SeedAsync();
            DomainEntities.CampaignCreator existing = new(10, 20, 30, 100m, 10m);
            db.Add(existing);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await service.UpdateCampaignCreator(existing.Id, new UpdateCampaignCreatorRequest
            {
                Id = existing.Id,
                CampaignCreatorStatusId = 31,
                AgreedAmount = 100m
            });

            (await db.Set<CampaignCreatorStatusHistory>().CountAsync(item => item.CampaignCreatorId == existing.Id)).Should().Be(1);
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateCampaignCreator_should_throw_when_target_status_not_found()
        {
            await SeedAsync();
            DomainEntities.CampaignCreator existing = new(10, 20, 30, 100m, 10m);
            db.Add(existing);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.UpdateCampaignCreator(existing.Id, new UpdateCampaignCreatorRequest
            {
                Id = existing.Id,
                CampaignCreatorStatusId = 999,
                AgreedAmount = 100m
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
