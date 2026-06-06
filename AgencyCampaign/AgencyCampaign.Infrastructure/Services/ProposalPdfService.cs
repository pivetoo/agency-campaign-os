using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalPdfService : IProposalPdfService
    {
        private readonly DbContext dbContext;

        public ProposalPdfService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<byte[]> GenerateForProposalAsync(long proposalId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await LoadProposalAsync(proposalId, cancellationToken);
            if (proposal is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            AgencySettings agency = await ResolveAgencyAsync(cancellationToken);
            string? template = await ResolveTemplateAsync(proposal, agency, cancellationToken);
            string html = ProposalHtmlBuilder.Build(proposal, agency, template);
            return await PuppeteerPdfRenderer.RenderToPdfAsync(html);
        }

        public async Task<byte[]?> GenerateForShareTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            ProposalShareLink? shareLink = await dbContext.Set<ProposalShareLink>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (shareLink is null || !shareLink.IsActive(DateTimeOffset.UtcNow))
            {
                return null;
            }

            Proposal? proposal = await LoadProposalAsync(shareLink.ProposalId, cancellationToken);
            if (proposal is null)
            {
                return null;
            }

            AgencySettings agency = await ResolveAgencyAsync(cancellationToken);
            string? template = await ResolveTemplateAsync(proposal, agency, cancellationToken);
            string html = ProposalHtmlBuilder.Build(proposal, agency, template);
            return await PuppeteerPdfRenderer.RenderToPdfAsync(html);
        }

        private async Task<Proposal?> LoadProposalAsync(long proposalId, CancellationToken cancellationToken)
        {
            return await dbContext.Set<Proposal>()
                .AsNoTracking()
                .Include(item => item.Opportunity)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.Items)
                    .ThenInclude(item => item.Creator)
                .Include(item => item.ProposalLayout)
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);
        }

        private async Task<string?> ResolveTemplateAsync(Proposal proposal, AgencySettings agency, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(proposal.ProposalLayout?.Template))
            {
                return proposal.ProposalLayout.Template;
            }

            ProposalTemplateVersion? defaultLayout = await dbContext.Set<ProposalTemplateVersion>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(defaultLayout?.Template))
            {
                return defaultLayout.Template;
            }

            return string.IsNullOrWhiteSpace(agency.ProposalHtmlTemplate) ? null : agency.ProposalHtmlTemplate;
        }

        private async Task<AgencySettings> ResolveAgencyAsync(CancellationToken cancellationToken)
        {
            return await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? new AgencySettings("Minha agência");
        }
    }
}
