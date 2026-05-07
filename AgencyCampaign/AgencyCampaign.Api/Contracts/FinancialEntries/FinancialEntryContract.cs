using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.FinancialEntries
{
    public sealed class FinancialEntryContract
    {
        public long Id { get; init; }

        public long AccountId { get; init; }

        public string? AccountName { get; init; }

        public string? AccountColor { get; init; }

        public long? CampaignId { get; init; }

        public string? CampaignName { get; init; }

        public long? CampaignDeliverableId { get; init; }

        public FinancialEntryType Type { get; init; }

        public FinancialEntryCategory Category { get; init; }

        public string Description { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public DateTimeOffset DueAt { get; init; }

        public DateTimeOffset OccurredAt { get; init; }

        public string? PaymentMethod { get; init; }

        public string? ReferenceCode { get; init; }

        public DateTimeOffset? PaidAt { get; init; }

        public FinancialEntryStatus Status { get; init; }

        public string? CounterpartyName { get; init; }

        public string? Notes { get; init; }

        public long? SubcategoryId { get; init; }

        public string? SubcategoryName { get; init; }

        public string? SubcategoryColor { get; init; }

        public long? ParentEntryId { get; init; }

        public int? InstallmentNumber { get; init; }

        public int? InstallmentTotal { get; init; }

        public string? InvoiceNumber { get; init; }

        public string? InvoiceUrl { get; init; }

        public DateTimeOffset? InvoiceIssuedAt { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<FinancialEntry, FinancialEntryContract>> Projection => item => new FinancialEntryContract
        {
            Id = item.Id,
            AccountId = item.AccountId,
            AccountName = item.Account == null ? null : item.Account.Name,
            AccountColor = item.Account == null ? null : item.Account.Color,
            CampaignId = item.CampaignId,
            CampaignName = item.Campaign == null ? null : item.Campaign.Name,
            CampaignDeliverableId = item.CampaignDeliverableId,
            Type = item.Type,
            Category = item.Category,
            Description = item.Description,
            Amount = item.Amount,
            DueAt = item.DueAt,
            OccurredAt = item.OccurredAt,
            PaymentMethod = item.PaymentMethod,
            ReferenceCode = item.ReferenceCode,
            PaidAt = item.PaidAt,
            Status = item.Status,
            CounterpartyName = item.CounterpartyName,
            Notes = item.Notes,
            SubcategoryId = item.SubcategoryId,
            SubcategoryName = item.Subcategory == null ? null : item.Subcategory.Name,
            SubcategoryColor = item.Subcategory == null ? null : item.Subcategory.Color,
            ParentEntryId = item.ParentEntryId,
            InstallmentNumber = item.InstallmentNumber,
            InstallmentTotal = item.InstallmentTotal,
            InvoiceNumber = item.InvoiceNumber,
            InvoiceUrl = item.InvoiceUrl,
            InvoiceIssuedAt = item.InvoiceIssuedAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
