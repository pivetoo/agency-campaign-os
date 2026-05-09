using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CreatorPayments
{
    public sealed class CreatorPaymentContract
    {
        public long Id { get; init; }
        public long CampaignCreatorId { get; init; }
        public long CreatorId { get; init; }
        public string? CreatorName { get; init; }
        public string? CreatorPixKey { get; init; }
        public PixKeyType? CreatorPixKeyType { get; init; }
        public long? CampaignId { get; init; }
        public string? CampaignName { get; init; }
        public long? CampaignDocumentId { get; init; }
        public decimal GrossAmount { get; init; }
        public decimal Discounts { get; init; }
        public decimal NetAmount { get; init; }
        public string? Description { get; init; }
        public PaymentMethod Method { get; init; }
        public PaymentStatus Status { get; init; }
        public string? Provider { get; init; }
        public string? ProviderTransactionId { get; init; }
        public string? PixKey { get; init; }
        public PixKeyType? PixKeyType { get; init; }
        public string? InvoiceNumber { get; init; }
        public string? InvoiceUrl { get; init; }
        public DateTimeOffset? InvoiceIssuedAt { get; init; }
        public DateTimeOffset? ScheduledFor { get; init; }
        public DateTimeOffset? PaidAt { get; init; }
        public DateTimeOffset? FailedAt { get; init; }
        public string? FailureReason { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public IReadOnlyCollection<CreatorPaymentEventContract> Events { get; init; } = [];

        public static Expression<Func<CreatorPayment, CreatorPaymentContract>> Projection => item => new CreatorPaymentContract
        {
            Id = item.Id,
            CampaignCreatorId = item.CampaignCreatorId,
            CreatorId = item.CreatorId,
            CreatorName = item.Creator != null ? item.Creator.Name : null,
            CreatorPixKey = item.Creator != null ? item.Creator.PixKey : null,
            CreatorPixKeyType = item.Creator != null ? item.Creator.PixKeyType : null,
            CampaignId = item.CampaignCreator != null ? item.CampaignCreator.CampaignId : (long?)null,
            CampaignName = item.CampaignCreator != null && item.CampaignCreator.Campaign != null ? item.CampaignCreator.Campaign.Name : null,
            CampaignDocumentId = item.CampaignDocumentId,
            GrossAmount = item.GrossAmount,
            Discounts = item.Discounts,
            NetAmount = item.NetAmount,
            Description = item.Description,
            Method = item.Method,
            Status = item.Status,
            Provider = item.Provider,
            ProviderTransactionId = item.ProviderTransactionId,
            PixKey = item.PixKey,
            PixKeyType = item.PixKeyType,
            InvoiceNumber = item.InvoiceNumber,
            InvoiceUrl = item.InvoiceUrl,
            InvoiceIssuedAt = item.InvoiceIssuedAt,
            ScheduledFor = item.ScheduledFor,
            PaidAt = item.PaidAt,
            FailedAt = item.FailedAt,
            FailureReason = item.FailureReason,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Events = item.Events.Select(e => new CreatorPaymentEventContract
            {
                Id = e.Id,
                EventType = e.EventType,
                OccurredAt = e.OccurredAt,
                Description = e.Description,
                Metadata = e.Metadata,
            }).ToList(),
        };
    }
}
