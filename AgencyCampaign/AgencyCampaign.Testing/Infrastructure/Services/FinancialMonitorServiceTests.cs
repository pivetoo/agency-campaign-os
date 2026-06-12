using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Services;
using Archon.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialMonitorServiceTests
    {
        private TestDbContext db = null!;
        private FinancialMonitorService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            FinancialEntryService entryService = new(db, new Mock<IAutomationDispatcher>().Object, new Mock<INotificationService>().Object, NullLogger<FinancialEntryService>.Instance);
            FinancialReportService reportService = new(db);
            service = new FinancialMonitorService(db, entryService, reportService);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<FinancialAccount> SeedAccountAsync(decimal initialBalance = 0m, string name = "Conta")
        {
            FinancialAccount account = new(name, FinancialAccountType.Bank, initialBalance, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();
            return account;
        }

        private async Task<FinancialEntry> SeedEntryAsync(long accountId, FinancialEntryType type, decimal amount, DateTimeOffset dueAt, FinancialEntryStatus status = FinancialEntryStatus.Pending)
        {
            FinancialEntryCategory category = type == FinancialEntryType.Receivable ? FinancialEntryCategory.BrandReceivable : FinancialEntryCategory.OperationalCost;
            FinancialEntry entry = new(accountId, type, category, "Lancamento", amount, dueAt, DateTimeOffset.UtcNow.AddDays(-1));
            if (status != FinancialEntryStatus.Pending)
            {
                entry.ChangeStatus(status, status == FinancialEntryStatus.Paid ? DateTimeOffset.UtcNow : null);
            }
            db.Add(entry);
            await db.SaveChangesAsync();
            return entry;
        }

        private async Task<CreatorPayment> SeedPaymentAsync(decimal grossAmount, DateTimeOffset? createdAt = null)
        {
            CreatorPayment payment = new(1, 7, grossAmount, 0m, PaymentMethod.Pix);
            payment.SetCreatedAt(createdAt ?? DateTimeOffset.UtcNow);
            db.Add(payment);
            await db.SaveChangesAsync();
            return payment;
        }

        private async Task SeedApprovalThresholdAsync(decimal threshold)
        {
            AgencySettings settings = new("Agencia");
            settings.Update("Agencia", null, null, null, null, null, null, null, null, threshold);
            db.Add(settings);
            await db.SaveChangesAsync();
        }

        private async Task ClosePreviousMonthAsync()
        {
            DateTimeOffset previous = DateTimeOffset.UtcNow.AddMonths(-1);
            FinancialPeriod period = new(previous.Year, previous.Month);
            period.Close(1);
            db.Add(period);
            await db.SaveChangesAsync();
        }

        [Test]
        public async Task GetMonitor_should_alert_cash_gap_when_projection_goes_negative()
        {
            FinancialAccount account = await SeedAccountAsync(initialBalance: 100m);
            await SeedEntryAsync(account.Id, FinancialEntryType.Payable, 500m, DateTimeOffset.UtcNow.AddDays(3));

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.CashGap);
            alert.Severity.Should().Be(MonitorAlertSeverity.Critical);
            alert.Amount.Should().Be(-400m);
            alert.ReferenceDate.Should().NotBeNull();
            monitor.Pulse.ProjectionNegativeAt.Should().NotBeNull();
            monitor.Pulse.ProjectedBalance30d.Should().Be(-400m);
        }

        [Test]
        public async Task GetMonitor_should_not_alert_cash_gap_when_projection_stays_positive()
        {
            FinancialAccount account = await SeedAccountAsync(initialBalance: 1000m);
            await SeedEntryAsync(account.Id, FinancialEntryType.Payable, 500m, DateTimeOffset.UtcNow.AddDays(3));

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Alerts.Should().NotContain(item => item.Type == MonitorAlertTypes.CashGap);
            monitor.Pulse.ProjectionNegativeAt.Should().BeNull();
        }

        [Test]
        public async Task GetMonitor_should_alert_overdue_receivable_with_worst_days()
        {
            FinancialAccount account = await SeedAccountAsync();
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 300m, DateTimeOffset.UtcNow.AddDays(-10));

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.OverdueReceivable);
            alert.Severity.Should().Be(MonitorAlertSeverity.Critical);
            alert.Count.Should().Be(1);
            alert.Amount.Should().Be(300m);
            alert.WorstDays.Should().BeGreaterThanOrEqualTo(9);
            monitor.Pulse.ReceivableOverdue.Should().Be(300m);
            monitor.Pulse.ReceivableOverdueCount.Should().Be(1);
        }

        [Test]
        public async Task GetMonitor_should_alert_overdue_payable_as_warning()
        {
            FinancialAccount account = await SeedAccountAsync();
            await SeedEntryAsync(account.Id, FinancialEntryType.Payable, 200m, DateTimeOffset.UtcNow.AddDays(-3));

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.OverduePayable);
            alert.Severity.Should().Be(MonitorAlertSeverity.Warning);
            alert.Amount.Should().Be(200m);
            monitor.Pulse.PayableOverdue.Should().Be(200m);
        }

        [Test]
        public async Task GetMonitor_should_alert_failed_payments()
        {
            CreatorPayment payment = await SeedPaymentAsync(100m);
            payment.MarkFailed("erro pix");
            await db.SaveChangesAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.PaymentFailed);
            alert.Severity.Should().Be(MonitorAlertSeverity.Critical);
            alert.Count.Should().Be(1);
            alert.Amount.Should().Be(100m);
        }

        [Test]
        public async Task GetMonitor_should_alert_due_next_48h_with_in_out_breakdown()
        {
            FinancialAccount account = await SeedAccountAsync(initialBalance: 10000m);
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 500m, DateTimeOffset.UtcNow.AddHours(24));
            await SeedEntryAsync(account.Id, FinancialEntryType.Payable, 200m, DateTimeOffset.UtcNow.AddHours(30));
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 900m, DateTimeOffset.UtcNow.AddDays(5));

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.DueNext48h);
            alert.Severity.Should().Be(MonitorAlertSeverity.Warning);
            alert.Count.Should().Be(2);
            alert.AmountIn.Should().Be(500m);
            alert.AmountOut.Should().Be(200m);
        }

        [Test]
        public async Task GetMonitor_should_alert_approval_stuck_after_48h()
        {
            await SeedApprovalThresholdAsync(100m);
            await SeedPaymentAsync(200m, createdAt: DateTimeOffset.UtcNow.AddDays(-3));

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.ApprovalStuck);
            alert.Severity.Should().Be(MonitorAlertSeverity.Warning);
            alert.Count.Should().Be(1);
            alert.Amount.Should().Be(200m);
        }

        [Test]
        public async Task GetMonitor_should_not_alert_approval_stuck_when_approved_or_recent()
        {
            await SeedApprovalThresholdAsync(100m);
            CreatorPayment approved = await SeedPaymentAsync(200m, createdAt: DateTimeOffset.UtcNow.AddDays(-3));
            approved.Approve(99);
            await db.SaveChangesAsync();
            await SeedPaymentAsync(300m, createdAt: DateTimeOffset.UtcNow);

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Alerts.Should().NotContain(item => item.Type == MonitorAlertTypes.ApprovalStuck);
        }

        [Test]
        public async Task GetMonitor_should_alert_reconciliation_backlog()
        {
            FinancialAccount account = await SeedAccountAsync(name: "Itau");
            FinancialEntry entry = await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 100m, DateTimeOffset.UtcNow.AddDays(10));
            BankTransaction matched = new(account.Id, "ext-1", DateTimeOffset.UtcNow, 100m, BankTransactionDirection.Credit, "ok");
            matched.AttachToEntry(entry.Id, BankTransactionMatchKind.Manual);
            db.Add(matched);
            db.Add(new BankTransaction(account.Id, "ext-2", DateTimeOffset.UtcNow, 50m, BankTransactionDirection.Debit, "pendente"));
            db.Add(new BankTransaction(account.Id, "ext-3", DateTimeOffset.UtcNow, 70m, BankTransactionDirection.Debit, "pendente"));
            await db.SaveChangesAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.ReconciliationBacklog);
            alert.Severity.Should().Be(MonitorAlertSeverity.Warning);
            alert.Count.Should().Be(2);
            alert.AccountId.Should().Be(account.Id);
            alert.AccountName.Should().Be("Itau");
        }

        [Test]
        public async Task GetMonitor_should_list_reconciliation_account_without_backlog_and_not_alert()
        {
            FinancialAccount account = await SeedAccountAsync(name: "Nubank");
            FinancialEntry entry = await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 100m, DateTimeOffset.UtcNow.AddDays(10));
            BankTransaction matched = new(account.Id, "ext-1", DateTimeOffset.UtcNow, 100m, BankTransactionDirection.Credit, "ok");
            matched.AttachToEntry(entry.Id, BankTransactionMatchKind.Manual);
            db.Add(matched);
            await db.SaveChangesAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Alerts.Should().NotContain(item => item.Type == MonitorAlertTypes.ReconciliationBacklog);
            MonitorReconciliationAccountModel summary = monitor.Reconciliation.Single();
            summary.AccountName.Should().Be("Nubank");
            summary.Pending.Should().Be(0);
            summary.LastImportAt.Should().NotBeNull();
        }

        [Test]
        public async Task GetMonitor_should_alert_period_open_after_grace_day()
        {
            Assume.That(DateTimeOffset.UtcNow.Day, Is.GreaterThan(5), "Regra so dispara apos o dia 5 do mes");
            await SeedAccountAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            MonitorAlertModel alert = monitor.Alerts.Single(item => item.Type == MonitorAlertTypes.PeriodOpen);
            alert.Severity.Should().Be(MonitorAlertSeverity.Info);
            monitor.Periods.Previous.IsClosed.Should().BeFalse();
        }

        [Test]
        public async Task GetMonitor_should_not_alert_period_open_when_previous_month_closed()
        {
            await SeedAccountAsync();
            await ClosePreviousMonthAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Alerts.Should().NotContain(item => item.Type == MonitorAlertTypes.PeriodOpen);
            monitor.Periods.Previous.IsClosed.Should().BeTrue();
            monitor.Periods.Current.IsClosed.Should().BeFalse();
        }

        [Test]
        public async Task GetMonitor_should_compute_pulse_real_balance_and_payout_queue()
        {
            FinancialAccount account = await SeedAccountAsync(initialBalance: 100m);
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 50m, DateTimeOffset.UtcNow.AddDays(-2), FinancialEntryStatus.Paid);
            await SeedPaymentAsync(100m);
            CreatorPayment scheduled = await SeedPaymentAsync(80m);
            scheduled.Schedule(DateTimeOffset.UtcNow.AddDays(1));
            await db.SaveChangesAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Pulse.RealBalance.Should().Be(150m);
            monitor.Pulse.PayoutQueueCount.Should().Be(2);
            monitor.Pulse.PayoutQueueAmount.Should().Be(180m);
        }

        [Test]
        public async Task GetMonitor_should_compute_pulse_open_totals_including_overdue()
        {
            FinancialAccount account = await SeedAccountAsync(initialBalance: 10000m);
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 700m, DateTimeOffset.UtcNow.AddDays(10));
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 300m, DateTimeOffset.UtcNow.AddDays(-5));
            await SeedEntryAsync(account.Id, FinancialEntryType.Payable, 400m, DateTimeOffset.UtcNow.AddDays(8));

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Pulse.ReceivableOpen.Should().Be(1000m);
            monitor.Pulse.ReceivableOverdue.Should().Be(300m);
            monitor.Pulse.PayableOpen.Should().Be(400m);
            monitor.Pulse.PayableOverdue.Should().Be(0m);
        }

        [Test]
        public async Task GetMonitor_should_group_upcoming_entries_by_day()
        {
            FinancialAccount account = await SeedAccountAsync(initialBalance: 10000m);
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 100m, DateTimeOffset.UtcNow.AddDays(1));
            await SeedEntryAsync(account.Id, FinancialEntryType.Payable, 40m, DateTimeOffset.UtcNow.AddDays(2));
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 999m, DateTimeOffset.UtcNow.AddDays(20));

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Upcoming.Should().HaveCount(2);
            MonitorUpcomingDayModel first = monitor.Upcoming.First();
            first.InCount.Should().Be(1);
            first.InAmount.Should().Be(100m);
            MonitorUpcomingDayModel second = monitor.Upcoming.Last();
            second.OutCount.Should().Be(1);
            second.OutAmount.Should().Be(40m);
            monitor.Upcoming.Should().BeInAscendingOrder(item => item.Date);
        }

        [Test]
        public async Task GetMonitor_should_bucket_payout_funnel()
        {
            await SeedApprovalThresholdAsync(100m);
            await SeedPaymentAsync(200m);
            await SeedPaymentAsync(50m);
            CreatorPayment scheduled = await SeedPaymentAsync(60m);
            scheduled.Schedule(DateTimeOffset.UtcNow.AddDays(1));
            CreatorPayment failed = await SeedPaymentAsync(70m);
            failed.MarkFailed("erro");
            await db.SaveChangesAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.PayoutFunnel.PendingApproval.Should().Be(1);
            monitor.PayoutFunnel.ReadyToPay.Should().Be(1);
            monitor.PayoutFunnel.Scheduled.Should().Be(1);
            monitor.PayoutFunnel.Failed.Should().Be(1);
        }

        [Test]
        public async Task GetMonitor_should_order_alerts_by_severity()
        {
            FinancialAccount account = await SeedAccountAsync(initialBalance: 10000m);
            await SeedEntryAsync(account.Id, FinancialEntryType.Receivable, 300m, DateTimeOffset.UtcNow.AddDays(-5));
            await SeedEntryAsync(account.Id, FinancialEntryType.Payable, 200m, DateTimeOffset.UtcNow.AddHours(24));

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Alerts.Should().HaveCountGreaterThanOrEqualTo(2);
            monitor.Alerts.First().Severity.Should().Be(MonitorAlertSeverity.Critical);
            monitor.Alerts.Should().BeInAscendingOrder(item => item.Severity);
        }

        [Test]
        public async Task GetMonitor_should_return_clean_state_when_nothing_requires_attention()
        {
            await SeedAccountAsync(initialBalance: 500m);
            await ClosePreviousMonthAsync();

            FinancialMonitorModel monitor = await service.GetMonitor();

            monitor.Alerts.Should().BeEmpty();
            monitor.Pulse.RealBalance.Should().Be(500m);
            monitor.Pulse.ReceivableOpen.Should().Be(0m);
            monitor.Pulse.PayoutQueueCount.Should().Be(0);
            monitor.Upcoming.Should().BeEmpty();
            monitor.Reconciliation.Should().BeEmpty();
            monitor.GeneratedAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
        }
    }
}
