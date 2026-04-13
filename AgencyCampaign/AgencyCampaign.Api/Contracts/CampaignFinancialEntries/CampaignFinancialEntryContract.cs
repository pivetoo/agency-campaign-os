using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignFinancialEntries
{
    public sealed class CampaignFinancialEntryContract
    {
        public long Id { get; init; }

        public long CampaignId { get; init; }

        public long? CampaignDeliverableId { get; init; }

        public CampaignFinancialEntryType Type { get; init; }

        public string Description { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public DateTimeOffset DueAt { get; init; }

        public DateTimeOffset? PaidAt { get; init; }

        public CampaignFinancialEntryStatus Status { get; init; }

        public string? CounterpartyName { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignFinancialEntry, CampaignFinancialEntryContract>> Projection => item => new CampaignFinancialEntryContract
        {
            Id = item.Id,
            CampaignId = item.CampaignId,
            CampaignDeliverableId = item.CampaignDeliverableId,
            Type = item.Type,
            Description = item.Description,
            Amount = item.Amount,
            DueAt = item.DueAt,
            PaidAt = item.PaidAt,
            Status = item.Status,
            CounterpartyName = item.CounterpartyName,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
