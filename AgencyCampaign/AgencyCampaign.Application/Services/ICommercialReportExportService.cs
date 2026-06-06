namespace AgencyCampaign.Application.Services
{
    public interface ICommercialReportExportService
    {
        Task<byte[]> ExportProposalsFunnel(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportBrandRanking(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportFunilPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportGanhosPerdasPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportForecastPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportMetasPdf(DateTimeOffset referenceDate, CancellationToken cancellationToken = default);

        Task<byte[]> ExportProposalsFunnelPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportBrandRankingPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    }
}
