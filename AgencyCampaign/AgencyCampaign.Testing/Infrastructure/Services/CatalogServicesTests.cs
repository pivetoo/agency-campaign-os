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
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
            service = new OpportunitySourceService(db);
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

        [Test]
        public async Task GetAll_should_filter_by_search()
        {
            db.Add(new OpportunitySource("Indicação", "#fff", 1));
            db.Add(new OpportunitySource("Inbound", "#fff", 2));
            await db.SaveChangesAsync();

            PagedResult<OpportunitySourceModel> result = await service.GetAll(new PagedRequest(), search: "indic", includeInactive: true);

            result.Items.Should().ContainSingle(item => item.Name == "Indicação");
        }

        [Test]
        public async Task Update_should_persist_changes()
        {
            OpportunitySource source = new("Old", "#fff", 1);
            db.Add(source);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateOpportunitySourceRequest request = new() { Id = source.Id, Name = "New", Color = "#000", DisplayOrder = 5, IsActive = false };

            OpportunitySourceModel result = await service.Update(source.Id, request);

            result.Name.Should().Be("New");
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task Delete_should_soft_delete_source_preserving_history()
        {
            OpportunitySource source = new("Old", "#fff", 1);
            db.Add(source);
            await db.SaveChangesAsync();

            await service.Delete(source.Id);

            db.ChangeTracker.Clear();
            OpportunitySource stored = await db.Set<OpportunitySource>().AsNoTracking().SingleAsync();
            stored.IsActive.Should().BeFalse();
            (await service.GetAll(new PagedRequest(), search: null, includeInactive: false)).Items.Should().BeEmpty();
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
            service = new OpportunityTagService(db);
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

        [Test]
        public async Task Update_should_throw_when_not_found()
        {
            UpdateOpportunityTagRequest request = new() { Id = 99, Name = "x", Color = "#fff" };

            Func<Task> act = () => service.Update(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_persist_changes()
        {
            OpportunityTag tag = new("vip", "#fff");
            db.Add(tag);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityTagModel result = await service.Update(tag.Id, new UpdateOpportunityTagRequest { Id = tag.Id, Name = "vip-edited", Color = "#000", IsActive = false });

            result.Name.Should().Be("vip-edited");
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetAll_should_filter_by_active_status()
        {
            db.Add(new OpportunityTag("active", "#fff"));
            OpportunityTag inactive = new("inactive", "#fff");
            inactive.Update("inactive", "#fff", false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            (await service.GetAll(new PagedRequest(), search: null, includeInactive: false)).Items.Should().ContainSingle(item => item.Name == "active");
            (await service.GetAll(new PagedRequest(), search: null, includeInactive: true)).Items.Should().HaveCount(2);
        }

        [Test]
        public async Task Delete_should_remove_tag()
        {
            OpportunityTag tag = new("vip", "#fff");
            db.Add(tag);
            await db.SaveChangesAsync();

            await service.Delete(tag.Id);

            (await db.Set<OpportunityTag>().CountAsync()).Should().Be(0);
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
            service = new DeliverableKindService(db);
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

        [Test]
        public async Task UpdateDeliverableKind_should_throw_when_not_found()
        {
            UpdateDeliverableKindRequest request = new() { Id = 99, Name = "x" };

            Func<Task> act = () => service.UpdateDeliverableKind(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateDeliverableKind_should_persist_changes()
        {
            DeliverableKind kind = new("Old");
            db.Add(kind);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            DeliverableKind result = await service.UpdateDeliverableKind(kind.Id, new UpdateDeliverableKindRequest { Id = kind.Id, Name = "New", DisplayOrder = 5, IsActive = false });

            result.Name.Should().Be("New");
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetDeliverableKindById_should_return_null_when_not_found()
        {
            (await service.GetDeliverableKindById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetDeliverableKinds_should_filter_by_search_term()
        {
            db.Add(new DeliverableKind("Stories"));
            db.Add(new DeliverableKind("Reels"));
            await db.SaveChangesAsync();

            PagedResult<DeliverableKind> result = await service.GetDeliverableKinds(new PagedRequest(), search: "ree", includeInactive: true);

            result.Items.Should().ContainSingle(item => item.Name == "Reels");
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
            service = new PlatformService(db);
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
            inactive.Update("Inactive", 0, false, null);
            db.Add(inactive);
            await db.SaveChangesAsync();

            List<Platform> result = await service.GetActivePlatforms();
            result.Should().ContainSingle(item => item.Name == "Active");
        }

        [Test]
        public async Task UpdatePlatform_should_throw_when_not_found()
        {
            UpdatePlatformRequest request = new() { Id = 99, Name = "x" };

            Func<Task> act = () => service.UpdatePlatform(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdatePlatform_should_persist_changes()
        {
            Platform platform = new("Old");
            db.Add(platform);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Platform result = await service.UpdatePlatform(platform.Id, new UpdatePlatformRequest { Id = platform.Id, Name = "New", DisplayOrder = 5, IsActive = false });

            result.Name.Should().Be("New");
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetPlatformById_should_return_null_when_not_found()
        {
            (await service.GetPlatformById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetPlatforms_should_filter_by_search_and_active()
        {
            db.Add(new Platform("Instagram"));
            db.Add(new Platform("TikTok"));
            await db.SaveChangesAsync();

            PagedResult<Platform> result = await service.GetPlatforms(new PagedRequest(), search: "tik", includeInactive: true);

            result.Items.Should().ContainSingle(item => item.Name == "TikTok");
        }
    }
}
