using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CreatorSocialHandleTests
    {
        [Test]
        public void Constructor_should_persist_handle_and_normalize_profile_url()
        {
            CreatorSocialHandle subject = new(creatorId: 1, platformId: 2, handle: "  @foo  ", profileUrl: "  https://x  ", followers: 1000, engagementRate: 5m, isPrimary: true);

            subject.Handle.Should().Be("@foo");
            subject.ProfileUrl.Should().Be("https://x");
            subject.IsPrimary.Should().BeTrue();
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_invalid_creator_or_platform_id()
        {
            Action invalidCreator = () => _ = new CreatorSocialHandle(0, 1, "@x");
            Action invalidPlatform = () => _ = new CreatorSocialHandle(1, 0, "@x");

            invalidCreator.Should().Throw<ArgumentOutOfRangeException>();
            invalidPlatform.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_should_reject_blank_handle()
        {
            Action act = () => _ = new CreatorSocialHandle(1, 2, " ");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Constructor_should_reject_negative_followers()
        {
            Action act = () => _ = new CreatorSocialHandle(1, 2, "@x", followers: -1);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase(-0.1)]
        [TestCase(100.1)]
        public void Constructor_should_reject_engagement_rate_out_of_range(decimal value)
        {
            Action act = () => _ = new CreatorSocialHandle(1, 2, "@x", engagementRate: value);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Update_should_replace_state_and_validate_inputs()
        {
            CreatorSocialHandle subject = new(1, 2, "@old");

            subject.Update(platformId: 3, handle: "  @new  ", profileUrl: "https://new", followers: 5000, engagementRate: 4.2m, isPrimary: true, isActive: false);

            subject.PlatformId.Should().Be(3);
            subject.Handle.Should().Be("@new");
            subject.IsActive.Should().BeFalse();
        }

        [Test]
        public void SyncAudience_should_update_followers_and_engagement()
        {
            CreatorSocialHandle subject = new(1, 1, "@fulano");

            subject.SyncAudience(48500, 3.2m);

            subject.Followers.Should().Be(48500);
            subject.EngagementRate.Should().Be(3.2m);
        }

        [Test]
        public void SyncAudience_should_preserve_engagement_when_only_followers_returned()
        {
            CreatorSocialHandle subject = new(1, 1, "@fulano", followers: 100, engagementRate: 5m);

            subject.SyncAudience(48500, null);

            subject.Followers.Should().Be(48500);
            subject.EngagementRate.Should().Be(5m);
        }

        [Test]
        public void SyncAudience_should_reject_negative_followers()
        {
            CreatorSocialHandle subject = new(1, 1, "@fulano");

            Action act = () => subject.SyncAudience(-1, null);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
