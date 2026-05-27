using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Domain
{
    [TestFixture]
    public sealed class DeliverableContentVersionTests
    {
        private static DeliverableContentVersion NewVersion(ReviewParticipant role = ReviewParticipant.Creator)
        {
            return new DeliverableContentVersion(10, 1, role, "Fulano", "nota");
        }

        [Test]
        public void New_version_starts_pending_internal_review()
        {
            NewVersion().Status.Should().Be(ContentVersionStatus.PendingInternalReview);
        }

        [Test]
        public void Brand_cannot_submit_a_version()
        {
            Action act = () => new DeliverableContentVersion(10, 1, ReviewParticipant.Brand, "Marca", null);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void SendToBrand_moves_to_pending_brand_review()
        {
            DeliverableContentVersion version = NewVersion();

            version.SendToBrand();

            version.Status.Should().Be(ContentVersionStatus.PendingBrandReview);
        }

        [Test]
        public void Approve_requires_pending_brand_review()
        {
            DeliverableContentVersion version = NewVersion();

            Action act = () => version.Approve();

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Approve_from_brand_review_sets_approved()
        {
            DeliverableContentVersion version = NewVersion();
            version.SendToBrand();

            version.Approve();

            version.Status.Should().Be(ContentVersionStatus.Approved);
        }

        [Test]
        public void RequestChanges_is_terminal_for_the_version()
        {
            DeliverableContentVersion version = NewVersion();
            version.SendToBrand();

            version.RequestChanges();

            version.Status.Should().Be(ContentVersionStatus.ChangesRequested);
        }

        [Test]
        public void AddAsset_appends_to_assets()
        {
            DeliverableContentVersion version = NewVersion();

            version.AddAsset(ContentAssetType.ExternalUrl, "https://x/y", null, null, 0);

            version.Assets.Should().HaveCount(1);
        }
    }
}
