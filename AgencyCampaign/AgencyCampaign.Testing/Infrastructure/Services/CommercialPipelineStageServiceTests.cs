using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CommercialPipelineStages;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CommercialPipelineStageServiceTests
    {
        private TestDbContext db = null!;
        private CommercialPipelineStageService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            IStringLocalizer<AgencyCampaignResource> localizer = LocalizerMock.Create<AgencyCampaignResource>();
            service = new CommercialPipelineStageService(db, localizer);
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
        }

        [Test]
        public async Task GetActiveStages_should_filter_inactive_and_order_by_display_then_name()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).WithName("B").WithDisplayOrder(2).Build());
            db.Add(new CommercialPipelineStageBuilder().WithId(2).WithName("A").WithDisplayOrder(2).Build());
            db.Add(new CommercialPipelineStageBuilder().WithId(3).WithName("C").WithDisplayOrder(1).Build());
            db.Add(new CommercialPipelineStageBuilder().WithId(4).WithName("Inactive").WithDisplayOrder(1).Inactive().Build());
            await db.SaveChangesAsync();

            List<CommercialPipelineStage> stages = await service.GetActiveStages();

            stages.Select(item => item.Name).Should().Equal("C", "A", "B");
        }

        [Test]
        public async Task GetStageById_should_return_null_when_not_found()
        {
            CommercialPipelineStage? result = await service.GetStageById(999);
            result.Should().BeNull();
        }

        [Test]
        public async Task GetStages_should_return_paged_result()
        {
            for (int i = 1; i <= 5; i++)
            {
                db.Add(new CommercialPipelineStageBuilder().WithId(i).WithName($"Stage {i}").WithDisplayOrder(i).Build());
            }
            await db.SaveChangesAsync();

            PagedResult<CommercialPipelineStage> result = await service.GetStages(new PagedRequest { Page = 1, PageSize = 3 });

            result.Pagination.TotalCount.Should().Be(5);
            result.Items.Should().HaveCount(3);
        }

        [Test]
        public async Task CreateStage_should_persist_new_stage()
        {
            CreateCommercialPipelineStageRequest request = new()
            {
                Name = "Qualificação",
                DisplayOrder = 1,
                Color = "#fff"
            };

            CommercialPipelineStage stage = await service.CreateStage(request);

            stage.Id.Should().BeGreaterThan(0);
            (await db.Set<CommercialPipelineStage>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task CreateStage_should_reject_when_initial_stage_already_exists()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).AsInitial().Build());
            await db.SaveChangesAsync();

            CreateCommercialPipelineStageRequest request = new()
            {
                Name = "Outro",
                DisplayOrder = 2,
                Color = "#fff",
                IsInitial = true
            };

            Func<Task> act = () => service.CreateStage(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateStage_should_throw_when_id_mismatch()
        {
            UpdateCommercialPipelineStageRequest request = new()
            {
                Id = 5,
                Name = "x",
                Color = "#fff"
            };

            Func<Task> act = () => service.UpdateStage(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateStage_should_throw_when_stage_not_found()
        {
            UpdateCommercialPipelineStageRequest request = new() { Id = 99, Name = "x", Color = "#fff" };

            Func<Task> act = () => service.UpdateStage(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateStage_should_allow_marking_existing_initial_stage_again()
        {
            CommercialPipelineStage stage = new CommercialPipelineStageBuilder().WithId(1).AsInitial().WithName("Inicial").Build();
            db.Add(stage);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCommercialPipelineStageRequest request = new()
            {
                Id = stage.Id,
                Name = "Inicial atualizada",
                DisplayOrder = 1,
                Color = "#fff",
                IsInitial = true,
                IsActive = true
            };

            CommercialPipelineStage result = await service.UpdateStage(stage.Id, request);

            result.Name.Should().Be("Inicial atualizada");
        }

        [Test]
        public async Task UpdateStage_should_block_when_marking_initial_while_another_is_initial()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).AsInitial().WithName("Existente").Build());
            CommercialPipelineStage another = new CommercialPipelineStageBuilder().WithId(2).WithName("Outro").Build();
            db.Add(another);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCommercialPipelineStageRequest request = new()
            {
                Id = another.Id,
                Name = "Outro",
                DisplayOrder = 3,
                Color = "#fff",
                IsInitial = true,
                IsActive = true
            };

            Func<Task> act = () => service.UpdateStage(another.Id, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
