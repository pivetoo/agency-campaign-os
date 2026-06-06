using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class PlanTierResolverTests
    {
        private TestDbContext db = null!;
        private PlanTierResolver subject = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            subject = new PlanTierResolver(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Should_return_internal_when_there_are_no_settings()
        {
            PlanTier tier = await subject.GetCurrentTierAsync();

            tier.Should().Be(PlanTier.Internal);
        }

        [Test]
        public async Task Should_return_the_stored_tier()
        {
            AgencySettings settings = new("Minha Agencia");
            settings.SetPlanTier(PlanTier.Scale);
            db.Add(settings);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            PlanTier tier = await subject.GetCurrentTierAsync();

            tier.Should().Be(PlanTier.Scale);
        }
    }
}
