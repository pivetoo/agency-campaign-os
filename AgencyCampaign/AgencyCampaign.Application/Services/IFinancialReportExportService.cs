using AgencyCampaign.Application.Models.Financial;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialReportExportService
    {
        Task<byte[]> ExportCashFlow(DateTimeOffset from, DateTimeOffset to, CashFlowGranularity granularity, CancellationToken cancellationToken = default);

        Task<byte[]> ExportAging(CancellationToken cancellationToken = default);

        Task<byte[]> ExportTaxWithholding(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCampaignProfitability(CancellationToken cancellationToken = default);

        Task<byte[]> ExportAccrualResult(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCashFlowProjection(int weeks, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCashFlowPdf(DateTimeOffset from, DateTimeOffset to, CashFlowGranularity granularity, CancellationToken cancellationToken = default);

        Task<byte[]> ExportAgingPdf(CancellationToken cancellationToken = default);

        Task<byte[]> ExportTaxWithholdingPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCampaignProfitabilityPdf(CancellationToken cancellationToken = default);

        Task<byte[]> ExportAccrualResultPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCashFlowProjectionPdf(int weeks, CancellationToken cancellationToken = default);
    }
}
