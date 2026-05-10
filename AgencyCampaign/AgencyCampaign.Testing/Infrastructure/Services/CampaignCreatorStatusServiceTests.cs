using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignCreatorStatuses;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using CampaignCreatorStatus = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignCreatorStatusServiceTests
    {
        private TestDbContext db = null!;
        private CampaignCreatorStatusService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CampaignCreatorStatusService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreateStatus_should_persist()
        {
            CreateCampaignCreatorStatusRequest request = new()
            {
                Name = "Confirmado",
                Color = "#22c55e",
                Category = CampaignCreatorStatusCategory.Success,
                MarksAsConfirmed = true
            };

            CampaignCreatorStatus status = await service.CreateStatus(request);

            status.Id.Should().BeGreaterThan(0);
            status.MarksAsConfirmed.Should().BeTrue();
        }

        [Test]
        public async Task CreateStatus_should_block_when_initial_already_exists()
        {
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).AsInitial().Build());
            await db.SaveChangesAsync();

            CreateCampaignCreatorStatusRequest request = new()
            {
                Name = "Outro",
                Color = "#fff",
                IsInitial = true
            };

            Func<Task> act = () => service.CreateStatus(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateStatus_should_throw_when_id_mismatch()
        {
            UpdateCampaignCreatorStatusRequest request = new() { Id = 5, Name = "x", Color = "#fff" };

            Func<Task> act = () => service.UpdateStatus(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateStatus_should_throw_when_not_found()
        {
            UpdateCampaignCreatorStatusRequest request = new() { Id = 99, Name = "x", Color = "#fff" };
            Func<Task> act = () => service.UpdateStatus(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateStatus_should_allow_keeping_existing_initial_marker()
        {
            CampaignCreatorStatus status = new CampaignCreatorStatusBuilder().WithId(1).AsInitial().WithName("Inicial").Build();
            db.Add(status);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignCreatorStatusRequest request = new()
            {
                Id = status.Id,
                Name = "Inicial",
                Color = "#fff",
                IsInitial = true,
                IsActive = true,
                Category = CampaignCreatorStatusCategory.InProgress
            };

            CampaignCreatorStatus result = await service.UpdateStatus(status.Id, request);

            result.IsInitial.Should().BeTrue();
        }

        [Test]
        public async Task GetActiveStatuses_should_only_return_active_records_ordered()
        {
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).WithName("B").Build());
            db.Add(new CampaignCreatorStatusBuilder().WithId(2).WithName("A").Build());
            await db.SaveChangesAsync();

            List<CampaignCreatorStatus> result = await service.GetActiveStatuses();

            result.Select(item => item.Name).Should().Equal("A", "B");
        }
    }
}
