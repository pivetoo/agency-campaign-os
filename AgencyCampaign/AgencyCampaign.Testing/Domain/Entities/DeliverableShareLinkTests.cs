using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class DeliverableShareLinkTests
    {
        [Test]
        public void Constructor_should_trim_inputs()
        {
            DeliverableShareLink subject = new(1, "  tok  ", "  Reviewer  ", DateTimeOffset.UtcNow.AddDays(1), 5, "  admin  ");

            subject.Token.Should().Be("tok");
            subject.ReviewerName.Should().Be("Reviewer");
            subject.CreatedByUserName.Should().Be("admin");
        }

        [Test]
        public void IsActive_should_be_false_when_revoked_or_expired()
        {
            DeliverableShareLink revoked = new(1, "tok", "r", null, null, null);
            revoked.Revoke();
            revoked.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();

            DeliverableShareLink expired = new(1, "tok", "r", DateTimeOffset.UtcNow.AddMinutes(-1), null, null);
            expired.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
        }

        [Test]
        public void IsActive_should_be_true_for_fresh_link_without_expiration()
        {
            DeliverableShareLink subject = new(1, "tok", "r", null, null, null);
            subject.IsActive(DateTimeOffset.UtcNow).Should().BeTrue();
        }

        [Test]
        public void RegisterView_should_increment_count_and_update_last_viewed_at()
        {
            DeliverableShareLink subject = new(1, "tok", "r", null, null, null);

            subject.RegisterView();
            subject.RegisterView();

            subject.ViewCount.Should().Be(2);
            subject.LastViewedAt.Should().NotBeNull();
        }

        [Test]
        public void Revoke_should_be_idempotent()
        {
            DeliverableShareLink subject = new(1, "tok", "r", null, null, null);
            subject.Revoke();
            DateTimeOffset firstRevocation = subject.RevokedAt!.Value;
            Thread.Sleep(2);
            subject.Revoke();

            subject.RevokedAt.Should().Be(firstRevocation);
        }
    }
}
