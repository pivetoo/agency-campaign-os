using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class DeliverableReviewComment : Entity
    {
        public long CampaignDeliverableId { get; private set; }
        public long? DeliverableContentVersionId { get; private set; }
        public ReviewParticipant AuthorRole { get; private set; }
        public string AuthorName { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        public ReviewCommentVisibility Visibility { get; private set; }

        private DeliverableReviewComment()
        {
        }

        public DeliverableReviewComment(long campaignDeliverableId, long? deliverableContentVersionId, ReviewParticipant authorRole, string authorName, string body, ReviewCommentVisibility visibility)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authorName);
            ArgumentException.ThrowIfNullOrWhiteSpace(body);
            if (authorRole != ReviewParticipant.Agency && visibility == ReviewCommentVisibility.Internal)
            {
                throw new InvalidOperationException("contentReview.comment.onlyAgencyInternal");
            }

            CampaignDeliverableId = campaignDeliverableId;
            DeliverableContentVersionId = deliverableContentVersionId;
            AuthorRole = authorRole;
            AuthorName = authorName.Trim();
            Body = body.Trim();
            Visibility = visibility;
        }
    }
}
