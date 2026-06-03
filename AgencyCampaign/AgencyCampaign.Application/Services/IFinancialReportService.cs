using AgencyCampaign.Application.Models.Financial;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialReportService
    {
        Task<CashFlowSeriesModel> GetCashFlow(DateTimeOffset from, DateTimeOffset to, CashFlowGranularity granularity, CancellationToken cancellationToken = default);

        Task<AgingReportModel> GetAgingReport(CancellationToken cancellationToken = default);

        Task<TaxWithholdingReportModel> GetTaxWithholdingReport(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    }
}
