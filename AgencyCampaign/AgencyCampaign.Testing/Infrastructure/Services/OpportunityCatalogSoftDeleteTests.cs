using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityCatalogSoftDeleteTests
    {
        private TestDbContext db = null!;

        [SetUp]
        public void SetUp() => db = TestDbContext.CreateInMemory();

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Delete_should_soft_delete_win_reason_preserving_history()
        {
            OpportunityWinReasonService service = new(db);
            OpportunityWinReason reason = new("Preco competitivo", "#fff", 1);
            db.Add(reason);
            await db.SaveChangesAsync();

            await service.Delete(reason.Id);

            db.ChangeTracker.Clear();
            OpportunityWinReason? stored = await db.Set<OpportunityWinReason>().AsNoTracking().FirstOrDefaultAsync(item => item.Id == reason.Id);
            stored.Should().NotBeNull();
            stored!.IsActive.Should().BeFalse();
        }
    }
}
