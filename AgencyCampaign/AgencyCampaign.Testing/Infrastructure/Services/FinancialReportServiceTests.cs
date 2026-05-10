using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialReportServiceTests
    {
        private TestDbContext db = null!;
        private FinancialReportService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new FinancialReportService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task GetCashFlow_should_split_pending_and_settled_buckets()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);

            FinancialEntry pendingReceivable = new(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "in", 500m, baseDate.AddDays(2), baseDate);
            FinancialEntry pendingPayable = new(1, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout,
                "out", 200m, baseDate.AddDays(2), baseDate);
            FinancialEntry settled = new(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "settled", 1000m, baseDate, baseDate);
            settled.ChangeStatus(FinancialEntryStatus.Paid, baseDate.AddDays(1));
            FinancialEntry cancelled = new(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "cancel", 9999m, baseDate, baseDate);
            cancelled.ChangeStatus(FinancialEntryStatus.Cancelled);

            db.Add(pendingReceivable);
            db.Add(pendingPayable);
            db.Add(settled);
            db.Add(cancelled);
            await db.SaveChangesAsync();

            CashFlowSeriesModel result = await service.GetCashFlow(baseDate.AddDays(-5), baseDate.AddDays(10), CashFlowGranularity.Day);

            result.Pending.Should().HaveCount(1);
            result.Pending.Single().Inflow.Should().Be(500m);
            result.Pending.Single().Outflow.Should().Be(200m);
            result.Settled.Should().HaveCount(1);
            result.Settled.Single().Inflow.Should().Be(1000m);
        }

        [Test]
        public async Task GetCashFlow_should_bucket_by_month_when_requested()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "a", 100m, baseDate, baseDate));
            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "b", 200m, baseDate.AddDays(5), baseDate));
            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "c", 300m, baseDate.AddMonths(1), baseDate));
            await db.SaveChangesAsync();

            CashFlowSeriesModel result = await service.GetCashFlow(baseDate.AddMonths(-1), baseDate.AddMonths(2), CashFlowGranularity.Month);

            result.Pending.Should().HaveCount(2);
            result.Pending.OrderBy(item => item.Bucket).First().Inflow.Should().Be(300m);
        }

        [Test]
        public async Task GetAgingReport_should_only_include_pending_and_overdue()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "due-future", 100m, now.AddDays(10), now));
            FinancialEntry overdue = new(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "overdue", 200m, now.AddDays(-15), now);
            overdue.ChangeStatus(FinancialEntryStatus.Overdue);
            db.Add(overdue);
            FinancialEntry paid = new(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "paid", 999m, now.AddDays(-1), now);
            paid.ChangeStatus(FinancialEntryStatus.Paid, now);
            db.Add(paid);
            await db.SaveChangesAsync();

            AgingReportModel result = await service.GetAgingReport();

            result.Buckets.Should().HaveCount(5);
            result.Buckets.Sum(item => item.TotalReceivable).Should().Be(300m);
        }
    }
}
