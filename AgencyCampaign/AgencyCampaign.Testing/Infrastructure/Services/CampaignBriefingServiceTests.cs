using AgencyCampaign.Application.Requests.CampaignBriefings;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignBriefingServiceTests
    {
        private TestDbContext db = null!;
        private CampaignBriefingService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CampaignBriefingService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task SeedCampaignAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "C", 0m, DateTimeOffset.UtcNow).WithId(10));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        [Test]
        public async Task Upsert_should_create_then_update_without_duplicating()
        {
            await SeedCampaignAsync();

            CampaignBriefingModel created = await service.Upsert(10, new UpsertCampaignBriefingRequest { KeyMessage = "v1", Dos = "fazer" });
            created.KeyMessage.Should().Be("v1");
            created.Dos.Should().Be("fazer");

            CampaignBriefingModel updated = await service.Upsert(10, new UpsertCampaignBriefingRequest { KeyMessage = "v2" });
            updated.KeyMessage.Should().Be("v2");
            updated.Dos.Should().BeNull();

            db.Set<CampaignBriefing>().Count(item => item.CampaignId == 10).Should().Be(1);
        }

        [Test]
        public async Task GetByCampaign_should_return_null_when_absent()
        {
            await SeedCampaignAsync();

            (await service.GetByCampaign(10)).Should().BeNull();
        }

        [Test]
        public async Task Upsert_should_reject_unknown_campaign()
        {
            Func<Task> act = async () => await service.Upsert(999, new UpsertCampaignBriefingRequest { KeyMessage = "x" });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
