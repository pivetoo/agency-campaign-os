using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Domain
{
    [TestFixture]
    public sealed class DeliverableReviewCommentTests
    {
        [Test]
        public void Agency_can_post_internal_comment()
        {
            DeliverableReviewComment comment = new(10, null, ReviewParticipant.Agency, "Op", "nota interna", ReviewCommentVisibility.Internal);

            comment.Visibility.Should().Be(ReviewCommentVisibility.Internal);
        }

        [Test]
        public void Brand_cannot_post_internal_comment()
        {
            Action act = () => new DeliverableReviewComment(10, null, ReviewParticipant.Brand, "Marca", "x", ReviewCommentVisibility.Internal);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Creator_cannot_post_internal_comment()
        {
            Action act = () => new DeliverableReviewComment(10, null, ReviewParticipant.Creator, "Creator", "x", ReviewCommentVisibility.Internal);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
