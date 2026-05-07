using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalPublicService : IProposalPublicService
    {
        private readonly DbContext dbContext;

        public ProposalPublicService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ProposalPublicViewModel?> GetByToken(string token, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            ProposalShareLink? shareLink = await dbContext.Set<ProposalShareLink>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (shareLink is null)
            {
                return null;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!shareLink.IsActive(now))
            {
                return null;
            }

            ProposalVersion? version = await dbContext.Set<ProposalVersion>()
                .AsNoTracking()
                .Where(item => item.ProposalId == shareLink.ProposalId)
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (version is null)
            {
                return null;
            }

            Proposal? proposal = await dbContext.Set<Proposal>()
                .AsNoTracking()
                .Include(item => item.Opportunity)
                    .ThenInclude(opp => opp!.Brand)
                .FirstOrDefaultAsync(item => item.Id == shareLink.ProposalId, cancellationToken);

            string brandName = proposal?.Opportunity?.Brand?.Name ?? string.Empty;

            shareLink.RegisterView(ipAddress, userAgent);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ProposalPublicViewModel
            {
                ProposalId = version.ProposalId,
                VersionNumber = version.VersionNumber,
                Name = version.Name,
                Description = version.Description,
                AgencyName = string.Empty,
                BrandName = brandName,
                TotalValue = version.TotalValue,
                ValidityUntil = version.ValidityUntil,
                SentAt = version.SentAt,
                SnapshotJson = version.SnapshotJson
            };
        }
    }
}
