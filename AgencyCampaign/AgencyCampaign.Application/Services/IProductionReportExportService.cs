namespace AgencyCampaign.Application.Services
{
    public interface IProductionReportExportService
    {
        Task<byte[]> ExportCampaignPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCreatorPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportPlatformProduction(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportDeliverableSla(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportApprovalCycle(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportContentLicenses(int expiringSoonDays, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCampaignPerformancePdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportCreatorPerformancePdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportPlatformProductionPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportDeliverableSlaPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportApprovalCyclePdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<byte[]> ExportContentLicensesPdf(int expiringSoonDays, CancellationToken cancellationToken = default);
    }
}
