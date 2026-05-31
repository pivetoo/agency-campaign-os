using AgencyCampaign.Application.Requests.Commercial;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Exceptions;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CommercialGoalServiceTests
    {
        private TestDbContext db = null!;
        private CommercialGoalService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CommercialGoalService(db, IdentityClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_throw_conflict_when_goal_already_exists_for_same_user_and_period()
        {
            DateTimeOffset periodStart = DateTimeOffset.UtcNow;
            db.Add(new CommercialGoal(userId: 5, CommercialGoalPeriodType.Month, periodStart, 1000m));
            await db.SaveChangesAsync();

            Func<Task> act = () => service.Create(new CreateCommercialGoalRequest
            {
                UserId = 5,
                PeriodType = (int)CommercialGoalPeriodType.Month,
                PeriodStart = periodStart,
                TargetAmount = 2000m
            });

            await act.Should().ThrowAsync<ConflictException>();
        }
    }
}
