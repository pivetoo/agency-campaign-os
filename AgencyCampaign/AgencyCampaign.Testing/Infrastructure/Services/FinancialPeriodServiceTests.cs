using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialPeriodServiceTests
    {
        private TestDbContext db = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private FinancialPeriodService Build(long? userId = 1)
        {
            return new FinancialPeriodService(db, CurrentUserMock.Create(userId: userId));
        }

        [Test]
        public async Task Close_should_create_and_close_period()
        {
            FinancialPeriodModel result = await Build().Close(2026, 5);

            result.IsClosed.Should().BeTrue();
            (await db.Set<FinancialPeriod>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Reopen_should_open_a_closed_period()
        {
            FinancialPeriodService service = Build();
            await service.Close(2026, 5);

            FinancialPeriodModel result = await service.Reopen(2026, 5);

            result.IsClosed.Should().BeFalse();
        }

        [Test]
        public async Task Close_should_throw_when_no_current_user()
        {
            FinancialPeriodService service = new(db, CurrentUserMock.Create(userId: null));

            Func<Task> act = () => service.Close(2026, 5);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialPeriod.userUnknown");
        }

        [Test]
        public async Task GetRecentPeriods_should_return_requested_months_with_status()
        {
            FinancialPeriodService service = Build();
            int year = DateTimeOffset.UtcNow.Year;
            int month = DateTimeOffset.UtcNow.Month;
            await service.Close(year, month);

            IReadOnlyList<FinancialPeriodModel> result = await service.GetRecentPeriods(6);

            result.Should().HaveCount(6);
            result.First(item => item.Year == year && item.Month == month).IsClosed.Should().BeTrue();
        }
    }
}
