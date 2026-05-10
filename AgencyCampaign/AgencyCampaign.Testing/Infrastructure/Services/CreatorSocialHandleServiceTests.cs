using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorSocialHandles;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CreatorSocialHandleServiceTests
    {
        private TestDbContext db = null!;
        private CreatorSocialHandleService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CreatorSocialHandleService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<(Creator creator, Platform platform)> SeedAsync()
        {
            Creator creator = new("Foo");
            Platform platform = new("Instagram");
            db.Add(creator);
            db.Add(platform);
            await db.SaveChangesAsync();
            return (creator, platform);
        }

        [Test]
        public async Task Create_should_throw_when_creator_not_found()
        {
            (_, Platform platform) = await SeedAsync();

            CreateCreatorSocialHandleRequest request = new() { CreatorId = 99, PlatformId = platform.Id, Handle = "@foo" };
            Func<Task> act = () => service.Create(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Create_should_throw_when_platform_not_found()
        {
            (Creator creator, _) = await SeedAsync();

            CreateCreatorSocialHandleRequest request = new() { CreatorId = creator.Id, PlatformId = 99, Handle = "@foo" };
            Func<Task> act = () => service.Create(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Create_should_persist_handle_and_return_with_platform_name()
        {
            (Creator creator, Platform platform) = await SeedAsync();

            var result = await service.Create(new CreateCreatorSocialHandleRequest
            {
                CreatorId = creator.Id,
                PlatformId = platform.Id,
                Handle = "@foo",
                Followers = 1000,
                EngagementRate = 5.5m,
                IsPrimary = true
            });

            result.PlatformName.Should().Be("Instagram");
            result.IsPrimary.Should().BeTrue();
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateCreatorSocialHandleRequest request = new() { Id = 5, CreatorId = 1, PlatformId = 1, Handle = "x" };
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
        public async Task GetByCreator_should_order_primary_first_then_by_platform_name()
        {
            (Creator creator, Platform ig) = await SeedAsync();
            Platform tk = new("TikTok");
            db.Add(tk);
            await db.SaveChangesAsync();

            CreatorSocialHandle ighandle = new(creator.Id, ig.Id, "@foo", null, null, null, isPrimary: false);
            CreatorSocialHandle tkhandle = new(creator.Id, tk.Id, "@foo", null, null, null, isPrimary: true);
            db.Add(ighandle);
            db.Add(tkhandle);
            await db.SaveChangesAsync();

            var result = await service.GetByCreator(creator.Id);

            result.First().PlatformName.Should().Be("TikTok");
            result.Last().PlatformName.Should().Be("Instagram");
        }
    }
}
