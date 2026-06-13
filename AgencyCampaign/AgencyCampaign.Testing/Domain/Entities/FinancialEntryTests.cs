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
                category: FinancialEntryCategory.BrandReceivable,
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
        public void RequestCharge_should_reject_a_payable()
        {
            FinancialEntry payable = new(1, FinancialEntryType.Payable, FinancialEntryCategory.OperationalCost, "Custo", 100m, DateTimeOffset.UtcNow.AddDays(5), DateTimeOffset.UtcNow);
            Action act = () => payable.RequestCharge("IntegrationPlatform");
            act.Should().Throw<InvalidOperationException>().WithMessage("financialEntry.charge.onlyReceivable");
        }

        [Test]
        public void RequestCharge_should_reject_a_paid_receivable()
        {
            FinancialEntry paid = BuildDefault();
            paid.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            Action act = () => paid.RequestCharge("IntegrationPlatform");
            act.Should().Throw<InvalidOperationException>().WithMessage("financialEntry.charge.notOpen");
        }

        [Test]
        public void RequestCharge_should_mark_requested_on_open_receivable()
        {
            FinancialEntry subject = BuildDefault();
            subject.RequestCharge("IntegrationPlatform");
            subject.ChargeStatus.Should().Be(ChargeStatus.Requested);
            subject.ChargeProvider.Should().Be("IntegrationPlatform");
        }

        [Test]
        public void MarkChargeIssued_should_store_link_and_status()
        {
            FinancialEntry subject = BuildDefault();
            subject.MarkChargeIssued("https://prov/boleto/1", DateTimeOffset.UtcNow);
            subject.ChargeUrl.Should().Be("https://prov/boleto/1");
            subject.ChargeStatus.Should().Be(ChargeStatus.Issued);
        }

        [Test]
        public void SettleFromCharge_should_pay_then_be_idempotent()
        {
            FinancialEntry subject = BuildDefault();
            bool first = subject.SettleFromCharge(DateTimeOffset.UtcNow, "E2E-1");
            bool second = subject.SettleFromCharge(DateTimeOffset.UtcNow, "E2E-1");

            first.Should().BeTrue();
            second.Should().BeFalse();
            subject.Status.Should().Be(FinancialEntryStatus.Paid);
            subject.ChargeStatus.Should().Be(ChargeStatus.Paid);
            subject.ReferenceCode.Should().Be("E2E-1");
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
            Action act = () => _ = new FinancialEntry(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "x", -1m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ChangeStatus_should_set_paid_at_when_status_is_paid()
        {
            FinancialEntry subject = BuildDefault();
            DateTimeOffset paidAt = DateTimeOffset.UtcNow.AddDays(-1);

            subject.ChangeStatus(FinancialEntryStatus.Paid, paidAt);

            subject.PaidAt.Should().Be(paidAt);
        }

        [Test]
        public void ChangeStatus_should_throw_when_reverting_a_paid_entry()
        {
            FinancialEntry subject = BuildDefault();
            subject.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow.AddDays(-1));

            Action act = () => subject.ChangeStatus(FinancialEntryStatus.Pending, DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Update_should_throw_when_entry_is_paid()
        {
            FinancialEntry subject = BuildDefault();
            subject.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow.AddDays(-1));

            Action act = () => subject.Update(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "alterado", 999m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, null, null, null, null);

            act.Should().Throw<InvalidOperationException>();
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

        [Test]
        public void MarkAsReversed_should_throw_when_not_paid()
        {
            FinancialEntry subject = BuildDefault();

            Action act = () => subject.MarkAsReversed(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("financialEntry.onlyPaidCanBeReversed");
        }

        [Test]
        public void MarkAsReversed_should_throw_when_already_reversed()
        {
            FinancialEntry subject = BuildDefault();
            subject.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            subject.MarkAsReversed(DateTimeOffset.UtcNow);

            Action act = () => subject.MarkAsReversed(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("financialEntry.alreadyReversed");
        }

        [Test]
        public void MarkAsReversed_should_set_flag_and_date()
        {
            FinancialEntry subject = BuildDefault();
            subject.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);

            subject.MarkAsReversed(DateTimeOffset.UtcNow);

            subject.IsReversed.Should().BeTrue();
            subject.ReversedAt.Should().NotBeNull();
        }

        [Test]
        public void BuildReversalEntry_should_create_opposite_paid_contra_entry()
        {
            FinancialEntry subject = BuildDefault();
            subject.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);

            FinancialEntry reversal = subject.BuildReversalEntry(DateTimeOffset.UtcNow, "estorno");

            reversal.Type.Should().Be(FinancialEntryType.Payable);
            reversal.Amount.Should().Be(subject.Amount);
            reversal.Status.Should().Be(FinancialEntryStatus.Paid);
            reversal.PaidAt.Should().NotBeNull();
            reversal.ReversalOfEntryId.Should().Be(subject.Id);
        }

        [Test]
        public void BuildReversalEntry_result_cannot_be_reversed_again()
        {
            FinancialEntry subject = BuildDefault();
            subject.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            FinancialEntry reversal = subject.BuildReversalEntry(DateTimeOffset.UtcNow, "estorno");

            Action act = () => reversal.MarkAsReversed(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("financialEntry.cannotReverseReversal");
        }
    }
}
