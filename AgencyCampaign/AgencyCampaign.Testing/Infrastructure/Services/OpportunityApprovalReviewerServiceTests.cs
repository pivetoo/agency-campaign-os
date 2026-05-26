using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityApprovalReviewerServiceTests
    {
        private TestDbContext db = null!;
        private OpportunityApprovalReviewerService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new OpportunityApprovalReviewerService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<long> SeedApprovalAsync()
        {
            OpportunityApprovalRequest approval = new(1, OpportunityApprovalType.DiscountApproval, "10%", "Tester");
            db.Add(approval);
            await db.SaveChangesAsync();
            return approval.Id;
        }

        [Test]
        public async Task Add_should_persist_reviewer()
        {
            long approvalId = await SeedApprovalAsync();

            await service.Add(approvalId, new AddOpportunityApprovalReviewerRequest { UserId = 10, UserName = "Ana", Required = true });

            (await db.Set<OpportunityApprovalReviewer>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Add_should_throw_when_same_user_already_reviewer()
        {
            long approvalId = await SeedApprovalAsync();
            await service.Add(approvalId, new AddOpportunityApprovalReviewerRequest { UserId = 10, UserName = "Ana", Required = true });

            Func<Task> act = () => service.Add(approvalId, new AddOpportunityApprovalReviewerRequest { UserId = 10, UserName = "Ana", Required = true });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
