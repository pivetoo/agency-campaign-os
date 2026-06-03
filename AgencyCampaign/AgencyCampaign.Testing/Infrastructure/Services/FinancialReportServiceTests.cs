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

        private async Task<FinancialAccount> SeedAccountAsync(decimal initialBalance, bool isActive = true)
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, initialBalance, "#fff");
            if (!isActive)
            {
                account.Update("Conta", FinancialAccountType.Bank, initialBalance, "#fff", null, null, null, null, isActive: false);
            }
            db.Add(account);
            await db.SaveChangesAsync();
            return account;
        }

        [Test]
        public async Task GetCashFlowProjection_opening_balance_should_match_active_account_derived_balance()
        {
            FinancialAccount active = await SeedAccountAsync(1000m);
            FinancialAccount inactive = await SeedAccountAsync(9999m, isActive: false);

            FinancialEntry paidReceivable = new(active.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "in", 500m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            paidReceivable.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            FinancialEntry paidPayable = new(active.Id, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout, "out", 200m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            paidPayable.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            FinancialEntry paidOnInactive = new(inactive.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "ghost", 7777m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            paidOnInactive.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            db.AddRange(paidReceivable, paidPayable, paidOnInactive);
            await db.SaveChangesAsync();

            CashFlowProjectionModel result = await service.GetCashFlowProjection(12);

            result.OpeningBalance.Should().Be(1300m);
        }

        [Test]
        public async Task GetCashFlowProjection_should_fold_overdue_into_current_week()
        {
            FinancialAccount account = await SeedAccountAsync(0m);
            FinancialEntry overdue = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "atrasado", 300m, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-10));
            db.Add(overdue);
            await db.SaveChangesAsync();

            CashFlowProjectionModel result = await service.GetCashFlowProjection(12);

            result.Series.First().Inflow.Should().Be(300m);
            result.Series.First().ProjectedBalance.Should().Be(300m);
        }

        [Test]
        public async Task GetCashFlowProjection_should_emit_all_weeks_and_carry_balance()
        {
            FinancialAccount account = await SeedAccountAsync(0m);
            FinancialEntry payable = new(account.Id, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout, "saida", 100m, DateTimeOffset.UtcNow.AddDays(14), DateTimeOffset.UtcNow);
            db.Add(payable);
            await db.SaveChangesAsync();

            CashFlowProjectionModel result = await service.GetCashFlowProjection(6);

            result.Series.Should().HaveCount(6);
            result.Series.Sum(item => item.Outflow).Should().Be(100m);
            result.Series.Last().ProjectedBalance.Should().Be(-100m);
        }

        [Test]
        public async Task GetCashFlowProjection_should_exclude_paid_and_cancelled_from_projection()
        {
            FinancialAccount account = await SeedAccountAsync(0m);
            FinancialEntry paid = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "pago", 500m, DateTimeOffset.UtcNow.AddDays(7), DateTimeOffset.UtcNow);
            paid.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            FinancialEntry cancelled = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "cancelado", 800m, DateTimeOffset.UtcNow.AddDays(7), DateTimeOffset.UtcNow);
            cancelled.ChangeStatus(FinancialEntryStatus.Cancelled);
            db.AddRange(paid, cancelled);
            await db.SaveChangesAsync();

            CashFlowProjectionModel result = await service.GetCashFlowProjection(6);

            result.Series.Sum(item => item.Inflow).Should().Be(0m);
        }

        [Test]
        public async Task GetCashFlowProjection_should_clamp_weeks_between_1_and_52()
        {
            await SeedAccountAsync(0m);

            CashFlowProjectionModel low = await service.GetCashFlowProjection(0);
            CashFlowProjectionModel high = await service.GetCashFlowProjection(100);

            low.Series.Should().HaveCount(1);
            high.Series.Should().HaveCount(52);
        }

        [Test]
        public async Task GetAccrualResult_should_exclude_reversed_pair()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            FinancialEntry original = new(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "rec", 1000m, now, now);
            original.ChangeStatus(FinancialEntryStatus.Paid, now);
            db.Add(original);
            await db.SaveChangesAsync();

            original.MarkAsReversed(now);
            FinancialEntry contra = original.BuildReversalEntry(now, "estorno");
            db.Add(contra);
            await db.SaveChangesAsync();

            AccrualResultModel result = await service.GetAccrualResult(now.AddDays(-1), now.AddDays(1));

            result.Revenue.Should().Be(0m);
            result.Expense.Should().Be(0m);
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

        [Test]
        public async Task GetAgingReport_should_not_double_count_entry_due_today()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "due-today", 100m, now.AddHours(-1), now));
            await db.SaveChangesAsync();

            AgingReportModel result = await service.GetAgingReport();

            result.Buckets.Sum(item => item.ReceivableCount).Should().Be(1);
            result.Buckets.Sum(item => item.TotalReceivable).Should().Be(100m);
        }

        [Test]
        public async Task GetTaxWithholdingReport_should_sum_withholding_by_creator()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
            Creator creator = new("Joana", document: "12345678000199", taxRegime: TaxRegime.SimplesNacional);
            db.Add(creator);
            await db.SaveChangesAsync();

            CreatorPayment p1 = new(1, creator.Id, 1000m, 0m, PaymentMethod.Pix, taxWithheld: 150m);
            p1.MarkPaid(baseDate);
            CreatorPayment p2 = new(1, creator.Id, 500m, 0m, PaymentMethod.Pix, taxWithheld: 50m);
            p2.MarkPaid(baseDate);
            CreatorPayment noWithholding = new(1, creator.Id, 200m, 0m, PaymentMethod.Pix);
            noWithholding.MarkPaid(baseDate);
            db.Add(p1);
            db.Add(p2);
            db.Add(noWithholding);
            await db.SaveChangesAsync();

            TaxWithholdingReportModel report = await service.GetTaxWithholdingReport(baseDate.AddDays(-1), baseDate.AddDays(1));

            report.Lines.Should().HaveCount(1);
            report.TotalWithheld.Should().Be(200m);
            report.Lines.First().TaxRegime.Should().Be(TaxRegime.SimplesNacional);
            report.Lines.First().TaxWithheld.Should().Be(200m);
        }

        [Test]
        public async Task GetCampaignProfitability_should_cross_revenue_and_creator_cost()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();
            Campaign campaign = new(brand.Id, "Camp", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();

            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "receita", 1000m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, campaignId: campaign.Id));
            db.Add(new FinancialEntry(1, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout, "repasse", 300m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, campaignId: campaign.Id));
            await db.SaveChangesAsync();

            CampaignProfitabilityReportModel report = await service.GetCampaignProfitability();

            report.Lines.Should().HaveCount(1);
            CampaignProfitabilityLineModel line = report.Lines.First();
            line.Revenue.Should().Be(1000m);
            line.CreatorCost.Should().Be(300m);
            line.Margin.Should().Be(700m);
            line.MarginPercent.Should().Be(70m);
        }

        [Test]
        public async Task GetAccrualResult_should_recognize_by_occurred_date()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "receita", 1000m, baseDate.AddDays(60), baseDate));
            db.Add(new FinancialEntry(1, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout, "despesa", 400m, baseDate.AddDays(60), baseDate));
            db.Add(new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "fora do periodo", 999m, baseDate, baseDate.AddMonths(2)));
            await db.SaveChangesAsync();

            AccrualResultModel result = await service.GetAccrualResult(baseDate.AddDays(-1), baseDate.AddDays(1));

            result.Revenue.Should().Be(1000m);
            result.Expense.Should().Be(400m);
            result.Result.Should().Be(600m);
        }
    }
}
