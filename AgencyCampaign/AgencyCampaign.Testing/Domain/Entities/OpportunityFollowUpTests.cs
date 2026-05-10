using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class OpportunityFollowUpTests
    {
        [Test]
        public void Constructor_should_trim_subject_and_normalize_due_at_to_utc()
        {
            OpportunityFollowUp subject = new(opportunityId: 1, subject: "  call  ", dueAt: DateTimeOffset.Now);

            subject.Subject.Should().Be("call");
            subject.DueAt.Offset.Should().Be(TimeSpan.Zero);
            subject.IsCompleted.Should().BeFalse();
        }

        [Test]
        public void Complete_should_set_completed_state()
        {
            OpportunityFollowUp subject = new(1, "call", DateTimeOffset.UtcNow);

            subject.Complete();

            subject.IsCompleted.Should().BeTrue();
            subject.CompletedAt.Should().NotBeNull();
        }

        [Test]
        public void Complete_should_throw_when_already_completed()
        {
            OpportunityFollowUp subject = new(1, "call", DateTimeOffset.UtcNow);
            subject.Complete();

            Action act = () => subject.Complete();
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
