using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Proposal : Entity
    {
        private readonly List<ProposalItem> items = [];
        private readonly List<ProposalStatusHistory> statusHistory = [];
        private readonly List<ProposalVersion> versions = [];
        private readonly List<ProposalShareLink> shareLinks = [];

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

        public decimal? DiscountPercent { get; private set; }

        public int? PaymentTermDays { get; private set; }

        public long? ProposalLayoutId { get; private set; }

        public ProposalTemplateVersion? ProposalLayout { get; private set; }

        public IReadOnlyCollection<ProposalItem> Items => items.AsReadOnly();

        public IReadOnlyCollection<ProposalStatusHistory> StatusHistory => statusHistory.AsReadOnly();

        public IReadOnlyCollection<ProposalVersion> Versions => versions.AsReadOnly();

        public IReadOnlyCollection<ProposalShareLink> ShareLinks => shareLinks.AsReadOnly();

        private Proposal()
        {
        }

        public Proposal(long opportunityId, string name, long internalOwnerId, string? description = null, DateTimeOffset? validityUntil = null, string? notes = null, long? createdByUserId = null, string? createdByUserName = null, decimal? discountPercent = null, int? paymentTermDays = null)
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
            DiscountPercent = ClampPercent(discountPercent);
            PaymentTermDays = ClampDays(paymentTermDays);
            Status = ProposalStatus.Draft;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;

            statusHistory.Add(new ProposalStatusHistory(
                Id, null, ProposalStatus.Draft, createdByUserId, createdByUserName, "Proposta criada"));
        }

        public void UpdateTotalValue(decimal total)
        {
            TotalValue = total;
        }

        public void Update(string name, DateTimeOffset? validityUntil, string? description, string? notes, long opportunityId, decimal? discountPercent = null, int? paymentTermDays = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);

            Name = name.Trim();
            Description = Normalize(description);
            ValidityUntil = validityUntil?.ToUniversalTime();
            OpportunityId = opportunityId;
            Notes = Normalize(notes);
            DiscountPercent = ClampPercent(discountPercent);
            PaymentTermDays = ClampDays(paymentTermDays);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void SetProposalLayout(long? layoutId)
        {
            ProposalLayoutId = layoutId;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ChangeStatus(ProposalStatus status, long? changedByUserId = null, string? changedByUserName = null, string? reason = null)
        {
            ApplyStatusChange(status, changedByUserId, changedByUserName, reason);
        }

        public void MarkAsSent(long? changedByUserId = null, string? changedByUserName = null, string? reason = null)
        {
            ApplyStatusChange(ProposalStatus.Sent, changedByUserId, changedByUserName, reason);
        }

        public void MarkAsViewed(long? changedByUserId = null, string? changedByUserName = null, string? reason = null)
        {
            if (Status != ProposalStatus.Sent)
            {
                throw new InvalidOperationException("proposal.markAsViewed.notSent");
            }

            ApplyStatusChange(ProposalStatus.Viewed, changedByUserId, changedByUserName, reason);
        }

        public void Approve(long? changedByUserId = null, string? changedByUserName = null, string? reason = null)
        {
            if (Status != ProposalStatus.Sent && Status != ProposalStatus.Viewed)
            {
                throw new InvalidOperationException("proposal.approve.notSentOrViewed");
            }

            ApplyStatusChange(ProposalStatus.Approved, changedByUserId, changedByUserName, reason);
        }

        public void Reject(long? changedByUserId = null, string? changedByUserName = null, string? reason = null)
        {
            if (Status != ProposalStatus.Sent && Status != ProposalStatus.Viewed)
            {
                throw new InvalidOperationException("proposal.reject.notSentOrViewed");
            }

            ApplyStatusChange(ProposalStatus.Rejected, changedByUserId, changedByUserName, reason);
        }

        public void ConvertToCampaign(long campaignId, long? changedByUserId = null, string? changedByUserName = null)
        {
            if (Status != ProposalStatus.Approved)
            {
                throw new InvalidOperationException("proposal.convert.notApproved");
            }

            CampaignId = campaignId;
            ApplyStatusChange(ProposalStatus.Converted, changedByUserId, changedByUserName, $"Convertida na campanha #{campaignId}");
        }

        public void Expire()
        {
            if (ValidityUntil.HasValue && ValidityUntil.Value < DateTimeOffset.UtcNow && Status == ProposalStatus.Sent)
            {
                ApplyStatusChange(ProposalStatus.Expired, null, null, "Validade expirada automaticamente");
            }
        }

        public void Cancel(long? changedByUserId = null, string? changedByUserName = null, string? reason = null)
        {
            if (Status == ProposalStatus.Converted)
            {
                throw new InvalidOperationException("proposal.cancel.alreadyConverted");
            }

            ApplyStatusChange(ProposalStatus.Cancelled, changedByUserId, changedByUserName, reason);
        }

        private void ApplyStatusChange(ProposalStatus newStatus, long? changedByUserId, string? changedByUserName, string? reason)
        {
            ProposalStatus previousStatus = Status;
            Status = newStatus;
            UpdatedAt = DateTimeOffset.UtcNow;

            if (previousStatus == newStatus)
            {
                return;
            }

            statusHistory.Add(new ProposalStatusHistory(
                Id, previousStatus, newStatus, changedByUserId, changedByUserName, reason));
        }

        public void AddItem(ProposalItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (items.Any(x => x.Id == item.Id))
            {
                throw new InvalidOperationException("proposal.item.alreadyExists");
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
                throw new InvalidOperationException("proposal.item.notFound");
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

        private static decimal? ClampPercent(decimal? value)
        {
            return value.HasValue ? Math.Clamp(value.Value, 0m, 100m) : null;
        }

        private static int? ClampDays(int? value)
        {
            return value.HasValue ? Math.Max(0, value.Value) : null;
        }
    }
}
