using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class FinancialPeriodTests
    {
        [Test]
        public void Constructor_should_reject_invalid_month()
        {
            Action act = () => _ = new FinancialPeriod(2026, 13);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Close_should_set_closed_state()
        {
            FinancialPeriod subject = new(2026, 5);

            subject.Close(userId: 7);

            subject.IsClosed.Should().BeTrue();
            subject.ClosedAt.Should().NotBeNull();
            subject.ClosedByUserId.Should().Be(7);
        }

        [Test]
        public void Close_should_throw_when_already_closed()
        {
            FinancialPeriod subject = new(2026, 5);
            subject.Close(userId: 7);

            Action act = () => subject.Close(userId: 8);

            act.Should().Throw<InvalidOperationException>().WithMessage("financialPeriod.alreadyClosed");
        }

        [Test]
        public void Reopen_should_open_a_closed_period()
        {
            FinancialPeriod subject = new(2026, 5);
            subject.Close(userId: 7);

            subject.Reopen(userId: 9);

            subject.IsClosed.Should().BeFalse();
            subject.ReopenedByUserId.Should().Be(9);
        }

        [Test]
        public void Reopen_should_throw_when_not_closed()
        {
            FinancialPeriod subject = new(2026, 5);

            Action act = () => subject.Reopen(userId: 9);

            act.Should().Throw<InvalidOperationException>().WithMessage("financialPeriod.notClosed");
        }
    }
}
