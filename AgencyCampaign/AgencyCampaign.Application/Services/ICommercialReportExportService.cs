namespace AgencyCampaign.Application.Services
{
    public interface ICommercialReportExportService
    {
        Task<byte[]> ExportProposalsFunnel(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportBrandRanking(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    }
}
