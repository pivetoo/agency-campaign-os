using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CreatorPaymentTests
    {
        [Test]
        public void Constructor_should_compute_net_amount()
        {
            CreatorPayment subject = new(campaignCreatorId: 1, creatorId: 2, grossAmount: 1000m, discounts: 250m, method: PaymentMethod.Pix);

            subject.NetAmount.Should().Be(750m);
            subject.Status.Should().Be(PaymentStatus.Pending);
        }

        [Test]
        public void Constructor_should_reject_discounts_greater_than_gross()
        {
            Action act = () => _ = new CreatorPayment(1, 2, grossAmount: 100m, discounts: 200m, method: PaymentMethod.Pix);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_recompute_net_amount()
        {
            CreatorPayment subject = new(1, 2, 1000m, 0m, PaymentMethod.Pix);

            subject.Update(grossAmount: 500m, discounts: 50m, method: PaymentMethod.Ted, description: null);

            subject.GrossAmount.Should().Be(500m);
            subject.NetAmount.Should().Be(450m);
            subject.Method.Should().Be(PaymentMethod.Ted);
        }

        [Test]
        public void Update_should_throw_when_payment_is_paid()
        {
            CreatorPayment subject = new(1, 2, 1000m, 0m, PaymentMethod.Pix);
            subject.MarkPaid(DateTimeOffset.UtcNow);

            Action act = () => subject.Update(500m, 0m, PaymentMethod.Pix, null);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Schedule_should_set_status_and_persist_scheduled_for_in_utc()
        {
            CreatorPayment subject = new(1, 2, 1000m, 0m, PaymentMethod.Pix);
            DateTimeOffset scheduled = DateTimeOffset.Now;

            subject.Schedule(scheduled);

            subject.Status.Should().Be(PaymentStatus.Scheduled);
            subject.ScheduledFor!.Value.Offset.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void MarkPaid_should_clear_failure_state()
        {
            CreatorPayment subject = new(1, 2, 1000m, 0m, PaymentMethod.Pix);
            subject.MarkFailed("network");

            subject.MarkPaid(DateTimeOffset.UtcNow);

            subject.Status.Should().Be(PaymentStatus.Paid);
            subject.FailureReason.Should().BeNull();
            subject.FailedAt.Should().BeNull();
        }

        [Test]
        public void Cancel_should_throw_when_payment_already_paid()
        {
            CreatorPayment subject = new(1, 2, 1000m, 0m, PaymentMethod.Pix);
            subject.MarkPaid(DateTimeOffset.UtcNow);

            Action act = () => subject.Cancel();
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Cancel_should_set_status_and_reason()
        {
            CreatorPayment subject = new(1, 2, 1000m, 0m, PaymentMethod.Pix);

            subject.Cancel("desistência");

            subject.Status.Should().Be(PaymentStatus.Cancelled);
            subject.FailureReason.Should().Be("desistência");
        }

        [Test]
        public void SnapshotPixDestination_should_persist_pix_data()
        {
            CreatorPayment subject = new(1, 2, 1000m, 0m, PaymentMethod.Pix);

            subject.SnapshotPixDestination("  foo@bar.com  ", PixKeyType.Email);

            subject.PixKey.Should().Be("foo@bar.com");
            subject.PixKeyType.Should().Be(PixKeyType.Email);
        }

        [Test]
        public void RegisterEvent_should_append_to_events_list()
        {
            CreatorPayment subject = new CreatorPayment(1, 2, 1000m, 0m, PaymentMethod.Pix).WithId(1);

            subject.RegisterEvent(CreatorPaymentEventType.Created);
            subject.RegisterEvent(CreatorPaymentEventType.Paid);

            subject.Events.Should().HaveCount(2);
        }
    }
}
