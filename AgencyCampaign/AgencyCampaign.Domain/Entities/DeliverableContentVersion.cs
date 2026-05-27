using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class DeliverableContentVersion : Entity
    {
        private readonly List<DeliverableContentAsset> assets = new();

        public long CampaignDeliverableId { get; private set; }
        public int RoundNumber { get; private set; }
        public ReviewParticipant SubmittedByRole { get; private set; }
        public string SubmittedByName { get; private set; } = string.Empty;
        public string? Note { get; private set; }
        public ContentVersionStatus Status { get; private set; }

        public IReadOnlyCollection<DeliverableContentAsset> Assets => assets.AsReadOnly();

        private DeliverableContentVersion()
        {
        }

        public DeliverableContentVersion(long campaignDeliverableId, int roundNumber, ReviewParticipant submittedByRole, string submittedByName, string? note)
        {
            if (submittedByRole == ReviewParticipant.Brand)
            {
                throw new InvalidOperationException("contentReview.version.brandCannotSubmit");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(submittedByName);
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            CampaignDeliverableId = campaignDeliverableId;
            RoundNumber = roundNumber;
            SubmittedByRole = submittedByRole;
            SubmittedByName = submittedByName.Trim();
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            Status = ContentVersionStatus.PendingInternalReview;
        }

        public void AddAsset(ContentAssetType type, string url, string? fileName, string? contentType, int displayOrder)
        {
            assets.Add(new DeliverableContentAsset(type, url, fileName, contentType, displayOrder));
        }

        public void SendToBrand()
        {
            if (Status != ContentVersionStatus.PendingInternalReview)
            {
                throw new InvalidOperationException("contentReview.version.invalidTransition");
            }

            Status = ContentVersionStatus.PendingBrandReview;
        }

        public void RequestChanges()
        {
            if (Status != ContentVersionStatus.PendingInternalReview && Status != ContentVersionStatus.PendingBrandReview)
            {
                throw new InvalidOperationException("contentReview.version.invalidTransition");
            }

            Status = ContentVersionStatus.ChangesRequested;
        }

        public void Approve()
        {
            if (Status != ContentVersionStatus.PendingBrandReview)
            {
                throw new InvalidOperationException("contentReview.version.invalidTransition");
            }

            Status = ContentVersionStatus.Approved;
        }
    }
}
