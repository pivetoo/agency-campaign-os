using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalVersionService : IProposalVersionService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalVersionService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<ProposalVersionModel>> GetByProposalId(long proposalId, CancellationToken cancellationToken = default)
        {
            bool exists = await dbContext.Set<Proposal>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == proposalId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return await dbContext.Set<ProposalVersion>()
                .AsNoTracking()
                .Where(item => item.ProposalId == proposalId)
                .OrderByDescending(item => item.VersionNumber)
                .Select(item => new ProposalVersionModel
                {
                    Id = item.Id,
                    ProposalId = item.ProposalId,
                    VersionNumber = item.VersionNumber,
                    Name = item.Name,
                    Description = item.Description,
                    TotalValue = item.TotalValue,
                    ValidityUntil = item.ValidityUntil,
                    SentAt = item.SentAt,
                    SentByUserId = item.SentByUserId,
                    SentByUserName = item.SentByUserName
                })
                .ToArrayAsync(cancellationToken);
        }

        public async Task<ProposalVersionDetailModel?> GetById(long versionId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<ProposalVersion>()
                .AsNoTracking()
                .Where(item => item.Id == versionId)
                .Select(item => new ProposalVersionDetailModel
                {
                    Id = item.Id,
                    ProposalId = item.ProposalId,
                    VersionNumber = item.VersionNumber,
                    Name = item.Name,
                    Description = item.Description,
                    TotalValue = item.TotalValue,
                    ValidityUntil = item.ValidityUntil,
                    SnapshotJson = item.SnapshotJson,
                    SentAt = item.SentAt,
                    SentByUserName = item.SentByUserName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
