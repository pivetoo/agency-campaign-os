using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignCreatorStatuses;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using CampaignCreatorStatus = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
            service = new CampaignCreatorStatusService(db);
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

        [Test]
        public async Task GetStatuses_should_filter_inactive_by_default()
        {
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).WithName("Active").Build());
            CampaignCreatorStatus inactive = new CampaignCreatorStatusBuilder().WithId(2).WithName("Inactive").Build();
            inactive.Update("Inactive", 0, "#fff", null, false, CampaignCreatorStatusCategory.InProgress, isActive: false, false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            Archon.Core.Pagination.PagedResult<CampaignCreatorStatus> result = await service.GetStatuses(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: false);

            result.Items.Should().ContainSingle(item => item.Name == "Active");
        }

        [Test]
        public async Task GetStatuses_should_filter_by_search()
        {
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).WithName("Confirmado").Build());
            db.Add(new CampaignCreatorStatusBuilder().WithId(2).WithName("Cancelado").Build());
            await db.SaveChangesAsync();

            Archon.Core.Pagination.PagedResult<CampaignCreatorStatus> result = await service.GetStatuses(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, search: "confirm", includeInactive: true);

            result.Items.Should().ContainSingle(item => item.Name == "Confirmado");
        }

        [Test]
        public async Task GetStatusById_should_return_null_when_not_found()
        {
            (await service.GetStatusById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetStatusById_should_return_status_when_found()
        {
            CampaignCreatorStatus status = new CampaignCreatorStatusBuilder().WithName("Confirmado").Build();
            db.Add(status);
            await db.SaveChangesAsync();

            CampaignCreatorStatus? result = await service.GetStatusById(status.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateStatus_should_throw_when_marking_initial_and_another_exists()
        {
            CampaignCreatorStatus existing = new CampaignCreatorStatusBuilder().WithId(1).WithName("Initial").AsInitial().Build();
            CampaignCreatorStatus other = new CampaignCreatorStatusBuilder().WithId(2).WithName("Other").Build();
            db.Add(existing);
            db.Add(other);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignCreatorStatusRequest request = new()
            {
                Id = other.Id,
                Name = "Other",
                Color = "#fff",
                IsInitial = true,
                Category = CampaignCreatorStatusCategory.InProgress
            };

            Func<Task> act = () => service.UpdateStatus(other.Id, request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("campaignCreatorStatus.initial.duplicate");
        }
    }
}
