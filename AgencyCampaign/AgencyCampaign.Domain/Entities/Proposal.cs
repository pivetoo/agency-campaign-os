using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Proposal : Entity
    {
        private readonly List<ProposalItem> items = [];

        public long? CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long OpportunityId { get; private set; }

        public Opportunity? Opportunity { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public ProposalStatus Status { get; private set; } = ProposalStatus.Draft;

        public DateTimeOffset? ValidityUntil { get; private set; }

        public decimal TotalValue { get; private set; }

        public long InternalOwnerId { get; private set; }

        public string? InternalOwnerName { get; private set; }

        public string? Notes { get; private set; }

        public IReadOnlyCollection<ProposalItem> Items => items.AsReadOnly();

        private Proposal()
        {
        }

        public Proposal(long opportunityId, string name, long internalOwnerId, string? description = null, DateTimeOffset? validityUntil = null, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(internalOwnerId);

            OpportunityId = opportunityId;
            Name = name.Trim();
            Description = Normalize(description);
            ValidityUntil = validityUntil?.ToUniversalTime();
            InternalOwnerId = internalOwnerId;
            Notes = Normalize(notes);
            Status = ProposalStatus.Draft;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void UpdateTotalValue(decimal total)
        {
            TotalValue = total;
        }

        public void Update(string name, DateTimeOffset? validityUntil, string? description, string? notes, long opportunityId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);

            Name = name.Trim();
            Description = Normalize(description);
            ValidityUntil = validityUntil?.ToUniversalTime();
            OpportunityId = opportunityId;
            Notes = Normalize(notes);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ChangeStatus(ProposalStatus status)
        {
            Status = status;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkAsSent()
        {
            Status = ProposalStatus.Sent;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkAsViewed()
        {
            if (Status != ProposalStatus.Sent)
            {
                throw new InvalidOperationException("Proposal must be Sent to be marked as Viewed.");
            }

            Status = ProposalStatus.Viewed;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Approve()
        {
            if (Status != ProposalStatus.Sent && Status != ProposalStatus.Viewed)
            {
                throw new InvalidOperationException("Proposal must be Sent or Viewed to be Approved.");
            }

            Status = ProposalStatus.Approved;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Reject()
        {
            if (Status != ProposalStatus.Sent && Status != ProposalStatus.Viewed)
            {
                throw new InvalidOperationException("Proposal must be Sent or Viewed to be Rejected.");
            }

            Status = ProposalStatus.Rejected;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ConvertToCampaign(long campaignId)
        {
            if (Status != ProposalStatus.Approved)
            {
                throw new InvalidOperationException("Proposal must be Approved to be Converted.");
            }

            CampaignId = campaignId;
            Status = ProposalStatus.Converted;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Expire()
        {
            if (ValidityUntil.HasValue && ValidityUntil.Value < DateTimeOffset.UtcNow && Status == ProposalStatus.Sent)
            {
                Status = ProposalStatus.Expired;
                UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        public void Cancel()
        {
            if (Status == ProposalStatus.Converted)
            {
                throw new InvalidOperationException("Cannot cancel a Converted proposal.");
            }

            Status = ProposalStatus.Cancelled;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void AddItem(ProposalItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (items.Any(x => x.Id == item.Id))
            {
                throw new InvalidOperationException("Item already exists in this proposal.");
            }

            items.Add(item);
            RecalculateTotal();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RemoveItem(long itemId)
        {
            var item = items.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                throw new InvalidOperationException("Item not found in this proposal.");
            }

            items.Remove(item);
            RecalculateTotal();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void SetInternalOwner(long userId, string userName)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(userName);

            InternalOwnerId = userId;
            InternalOwnerName = userName;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private void RecalculateTotal()
        {
            TotalValue = items.Sum(x => x.Total);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
