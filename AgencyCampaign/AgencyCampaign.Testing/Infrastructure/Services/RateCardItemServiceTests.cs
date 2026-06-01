using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class RateCardItemServiceTests
    {
        private TestDbContext db = null!;
        private RateCardItemService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new RateCardItemService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Creator> SeedCreatorAsync()
        {
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();
            return creator;
        }

        [Test]
        public async Task Create_then_GetByCreator_should_return_active_item()
        {
            Creator creator = await SeedCreatorAsync();

            await service.Create(new CreateRateCardItemRequest { CreatorId = creator.Id, Label = "Reel", UnitPrice = 3000m, DisplayOrder = 1 });

            var items = await service.GetByCreator(creator.Id, includeInactive: false);
            items.Should().ContainSingle(item => item.Label == "Reel" && item.UnitPrice == 3000m && item.IsActive);
        }

        [Test]
        public async Task Create_should_throw_when_creator_not_found()
        {
            Func<Task> act = () => service.Create(new CreateRateCardItemRequest { CreatorId = 999, Label = "Reel", UnitPrice = 100m });
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Test]
        public async Task Delete_should_soft_delete_and_hide_from_default_listing()
        {
            Creator creator = await SeedCreatorAsync();
            var created = await service.Create(new CreateRateCardItemRequest { CreatorId = creator.Id, Label = "Stories", UnitPrice = 800m });

            await service.Delete(created.Id);

            (await service.GetByCreator(creator.Id, includeInactive: false)).Should().BeEmpty();
            (await service.GetByCreator(creator.Id, includeInactive: true)).Should().ContainSingle();
        }
    }
}
