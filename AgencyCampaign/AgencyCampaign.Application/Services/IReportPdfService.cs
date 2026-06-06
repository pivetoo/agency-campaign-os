using AgencyCampaign.Application.Models.Reports;

namespace AgencyCampaign.Application.Services
{
    public interface IReportPdfService
    {
        Task<byte[]> GenerateAsync(ReportTable table, CancellationToken cancellationToken = default);
    }
}
