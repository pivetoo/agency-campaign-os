using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ReportPdfService : IReportPdfService
    {
        private readonly DbContext dbContext;

        public ReportPdfService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<byte[]> GenerateAsync(ReportTable table, CancellationToken cancellationToken = default)
        {
            AgencySettings agency = await dbContext.Set<AgencySettings>().AsNoTracking().OrderBy(a => a.Id).FirstOrDefaultAsync(cancellationToken) ?? new AgencySettings("Minha agência");
            string html = ReportHtmlBuilder.Build(table, agency);
            return await PuppeteerPdfRenderer.RenderToPdfAsync(html);
        }
    }
}
