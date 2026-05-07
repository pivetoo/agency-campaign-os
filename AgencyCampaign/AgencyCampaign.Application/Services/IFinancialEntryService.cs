using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialEntries;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public sealed class FinancialEntryFilters
    {
        public FinancialEntryType? Type { get; set; }
        public FinancialEntryStatus? Status { get; set; }
        public long? AccountId { get; set; }
        public long? CampaignId { get; set; }
        public DateTimeOffset? DueFrom { get; set; }
        public DateTimeOffset? DueTo { get; set; }
        public string? Search { get; set; }
    }

    public interface IFinancialEntryService : ICrudService<FinancialEntry>
    {
        Task<PagedResult<FinancialEntry>> GetEntries(PagedRequest request, FinancialEntryFilters filters, CancellationToken cancellationToken = default);

        Task<FinancialEntry?> GetEntryById(long id, CancellationToken cancellationToken = default);

        Task<List<FinancialEntry>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default);

        Task<FinancialEntry> CreateEntry(CreateFinancialEntryRequest request, CancellationToken cancellationToken = default);

        Task<FinancialEntry> UpdateEntry(long id, UpdateFinancialEntryRequest request, CancellationToken cancellationToken = default);

        Task<FinancialEntry> MarkAsPaid(long id, MarkAsPaidRequest request, CancellationToken cancellationToken = default);

        Task<FinancialSummaryModel> GetSummary(FinancialEntryType type, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<FinancialEntry>> CreateInstallmentSeries(CreateInstallmentSeriesRequest request, CancellationToken cancellationToken = default);
    }
}
