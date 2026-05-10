using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CreatorAccessTokenTests
    {
        [Test]
        public void Constructor_should_trim_token_and_normalize_optional_fields()
        {
            CreatorAccessToken subject = new(creatorId: 1, token: "  abc  ", expiresAt: DateTimeOffset.UtcNow.AddDays(1), note: " ", createdByUserId: 5, createdByUserName: " admin ");

            subject.Token.Should().Be("abc");
            subject.Note.Should().BeNull();
            subject.CreatedByUserName.Should().Be("admin");
            subject.UsageCount.Should().Be(0);
        }

        [Test]
        public void IsValid_should_be_false_when_revoked()
        {
            CreatorAccessToken subject = new(1, "abc");
            subject.Revoke();

            subject.IsValid(DateTimeOffset.UtcNow).Should().BeFalse();
        }

        [Test]
        public void IsValid_should_be_false_when_expired()
        {
            CreatorAccessToken subject = new(1, "abc", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

            subject.IsValid(DateTimeOffset.UtcNow).Should().BeFalse();
        }

        [Test]
        public void IsValid_should_be_true_when_not_revoked_and_not_expired()
        {
            CreatorAccessToken subject = new(1, "abc", expiresAt: DateTimeOffset.UtcNow.AddDays(1));

            subject.IsValid(DateTimeOffset.UtcNow).Should().BeTrue();
        }

        [Test]
        public void IsValid_should_be_true_when_no_expiration()
        {
            CreatorAccessToken subject = new(1, "abc");

            subject.IsValid(DateTimeOffset.UtcNow).Should().BeTrue();
        }

        [Test]
        public void Revoke_should_be_idempotent()
        {
            CreatorAccessToken subject = new(1, "abc");
            subject.Revoke();
            DateTimeOffset firstRevocation = subject.RevokedAt!.Value;
            Thread.Sleep(2);

            subject.Revoke();

            subject.RevokedAt.Should().Be(firstRevocation);
        }

        [Test]
        public void RegisterUse_should_increment_count_and_set_last_used_at()
        {
            CreatorAccessToken subject = new(1, "abc");

            subject.RegisterUse();
            subject.RegisterUse();

            subject.UsageCount.Should().Be(2);
            subject.LastUsedAt.Should().NotBeNull();
        }

        [Test]
        public void UpdateExpiration_should_overwrite_existing_value()
        {
            CreatorAccessToken subject = new(1, "abc", expiresAt: DateTimeOffset.UtcNow.AddDays(1));
            DateTimeOffset newExpiry = DateTimeOffset.UtcNow.AddDays(7);

            subject.UpdateExpiration(newExpiry);

            subject.ExpiresAt.Should().BeCloseTo(newExpiry, TimeSpan.FromMilliseconds(50));
        }
    }
}
