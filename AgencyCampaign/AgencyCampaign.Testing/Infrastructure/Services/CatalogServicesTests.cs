using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.DeliverableKinds;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Requests.Platforms;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunitySourceServiceTests
    {
        private TestDbContext db = null!;
        private OpportunitySourceService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new OpportunitySourceService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_persist()
        {
            OpportunitySourceModel result = await service.Create(new CreateOpportunitySourceRequest { Name = "Indicação", Color = "#fff" });
            result.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateOpportunitySourceRequest request = new() { Id = 5, Name = "x", Color = "#fff" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_throw_when_not_found()
        {
            UpdateOpportunitySourceRequest request = new() { Id = 99, Name = "x", Color = "#fff" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetAll_should_filter_inactive_when_requested()
        {
            db.Add(new OpportunitySource("Active", "#fff", 1));
            OpportunitySource inactive = new("Inactive", "#fff", 2);
            inactive.Update("Inactive", "#fff", 2, false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            (await service.GetAll(new PagedRequest(), search: null, includeInactive: false)).Items.Should().ContainSingle();
            (await service.GetAll(new PagedRequest(), search: null, includeInactive: true)).Items.Should().HaveCount(2);
        }

        [Test]
        public async Task Delete_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Delete(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }

    [TestFixture]
    public sealed class OpportunityTagServiceTests
    {
        private TestDbContext db = null!;
        private OpportunityTagService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new OpportunityTagService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_persist()
        {
            OpportunityTagModel result = await service.Create(new CreateOpportunityTagRequest { Name = "vip", Color = "#fff" });
            result.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateOpportunityTagRequest request = new() { Id = 5, Name = "x", Color = "#fff" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Delete_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Delete(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }

    [TestFixture]
    public sealed class DeliverableKindServiceTests
    {
        private TestDbContext db = null!;
        private DeliverableKindService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new DeliverableKindService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreateDeliverableKind_should_persist()
        {
            DeliverableKind result = await service.CreateDeliverableKind(new CreateDeliverableKindRequest { Name = "Story", DisplayOrder = 1 });
            result.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task UpdateDeliverableKind_should_throw_when_id_mismatch()
        {
            UpdateDeliverableKindRequest request = new() { Id = 5, Name = "x" };
            Func<Task> act = () => service.UpdateDeliverableKind(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetActiveDeliverableKinds_should_filter_inactive()
        {
            db.Add(new DeliverableKind("Active"));
            DeliverableKind inactive = new("Inactive");
            inactive.Update("Inactive", 0, false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            List<DeliverableKind> result = await service.GetActiveDeliverableKinds();
            result.Should().ContainSingle(item => item.Name == "Active");
        }
    }

    [TestFixture]
    public sealed class PlatformServiceTests
    {
        private TestDbContext db = null!;
        private PlatformService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new PlatformService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreatePlatform_should_persist()
        {
            Platform result = await service.CreatePlatform(new CreatePlatformRequest { Name = "Instagram", DisplayOrder = 1 });
            result.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task UpdatePlatform_should_throw_when_id_mismatch()
        {
            UpdatePlatformRequest request = new() { Id = 5, Name = "x" };
            Func<Task> act = () => service.UpdatePlatform(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetActivePlatforms_should_filter_inactive()
        {
            db.Add(new Platform("Active"));
            Platform inactive = new("Inactive");
            inactive.Update("Inactive", 0, false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            List<Platform> result = await service.GetActivePlatforms();
            result.Should().ContainSingle(item => item.Name == "Active");
        }
    }
}
