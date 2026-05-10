using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class FinancialEntryTests
    {
        private static FinancialEntry BuildDefault(DateTimeOffset? dueAt = null, FinancialEntryStatus? overrideStatus = null)
        {
            FinancialEntry entry = new(
                accountId: 1,
                type: FinancialEntryType.Receivable,
                category: FinancialEntryCategory.CampaignRevenue,
                description: "  pagamento  ",
                amount: 100m,
                dueAt: dueAt ?? DateTimeOffset.UtcNow.AddDays(5),
                occurredAt: DateTimeOffset.UtcNow);

            if (overrideStatus.HasValue)
            {
                entry.ChangeStatus(overrideStatus.Value);
            }

            return entry;
        }

        [Test]
        public void Constructor_should_trim_description_and_default_status_to_pending()
        {
            FinancialEntry subject = BuildDefault();

            subject.Description.Should().Be("pagamento");
            subject.Status.Should().Be(FinancialEntryStatus.Pending);
            subject.DueAt.Offset.Should().Be(TimeSpan.Zero);
            subject.OccurredAt.Offset.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void Constructor_should_reject_negative_amount()
        {
            Action act = () => _ = new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.CampaignRevenue,
                "x", -1m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ChangeStatus_should_set_paid_at_only_when_status_is_paid()
        {
            FinancialEntry subject = BuildDefault();
            DateTimeOffset paidAt = DateTimeOffset.UtcNow.AddDays(-1);

            subject.ChangeStatus(FinancialEntryStatus.Paid, paidAt);
            subject.PaidAt.Should().Be(paidAt);

            subject.ChangeStatus(FinancialEntryStatus.Pending, DateTimeOffset.UtcNow);
            subject.PaidAt.Should().BeNull();
        }

        [Test]
        public void RecalculateOverdue_should_promote_pending_to_overdue_when_due_passed()
        {
            FinancialEntry subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(-1));

            subject.RecalculateOverdue(DateTimeOffset.UtcNow);

            subject.Status.Should().Be(FinancialEntryStatus.Overdue);
        }

        [Test]
        public void RecalculateOverdue_should_revert_overdue_to_pending_when_due_back_in_future()
        {
            FinancialEntry subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(-1));
            subject.ChangeStatus(FinancialEntryStatus.Overdue);

            FinancialEntry future = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(5));
            future.ChangeStatus(FinancialEntryStatus.Overdue);

            future.RecalculateOverdue(DateTimeOffset.UtcNow);

            future.Status.Should().Be(FinancialEntryStatus.Pending);
        }

        [Test]
        public void RecalculateOverdue_should_not_change_paid_entries()
        {
            FinancialEntry subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(-10));
            subject.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow.AddDays(-9));

            subject.RecalculateOverdue(DateTimeOffset.UtcNow);

            subject.Status.Should().Be(FinancialEntryStatus.Paid);
        }

        [Test]
        public void MarkAsInstallment_should_reject_installment_number_greater_than_total()
        {
            FinancialEntry subject = BuildDefault();
            Action act = () => subject.MarkAsInstallment(null, 5, 3);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void MarkAsInstallment_should_persist_metadata()
        {
            FinancialEntry subject = BuildDefault();

            subject.MarkAsInstallment(parentEntryId: 99, installmentNumber: 2, installmentTotal: 5);

            subject.ParentEntryId.Should().Be(99);
            subject.InstallmentNumber.Should().Be(2);
            subject.InstallmentTotal.Should().Be(5);
        }

        [Test]
        public void LinkToProposal_should_reject_invalid_id()
        {
            FinancialEntry subject = BuildDefault();
            Action act = () => subject.LinkToProposal(0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
