using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ProposalShareLinkTests
    {
        [Test]
        public void IsActive_should_consider_revocation_and_expiration()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            ProposalShareLink fresh = new(1, "tok", null, null, null);
            fresh.IsActive(now).Should().BeTrue();

            ProposalShareLink expired = new(1, "tok", now.AddMinutes(-5), null, null);
            expired.IsActive(now).Should().BeFalse();

            ProposalShareLink revoked = new(1, "tok", now.AddDays(1), null, null);
            revoked.Revoke();
            revoked.IsActive(now).Should().BeFalse();
        }

        [Test]
        public void RegisterView_should_append_view_and_update_counters()
        {
            ProposalShareLink subject = new(1, "tok", null, null, null);

            subject.RegisterView("1.2.3.4", "ua");
            subject.RegisterView(null, null);

            subject.Views.Should().HaveCount(2);
            subject.ViewCount.Should().Be(2);
            subject.LastViewedAt.Should().NotBeNull();
        }

        [Test]
        public void Constructor_should_reject_invalid_inputs()
        {
            Action invalidProposal = () => _ = new ProposalShareLink(0, "tok", null, null, null);
            Action blankToken = () => _ = new ProposalShareLink(1, " ", null, null, null);

            invalidProposal.Should().Throw<ArgumentOutOfRangeException>();
            blankToken.Should().Throw<ArgumentException>();
        }
    }
}
