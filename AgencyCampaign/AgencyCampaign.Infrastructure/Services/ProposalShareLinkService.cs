using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalShareLinkService : IProposalShareLinkService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;

        public ProposalShareLinkService(DbContext dbContext, ICurrentUser currentUser)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
        }

        public async Task<IReadOnlyCollection<ProposalShareLinkModel>> GetByProposalId(long proposalId, CancellationToken cancellationToken = default)
        {
            await EnsureProposalExists(proposalId, cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            return await dbContext.Set<ProposalShareLink>()
                .AsNoTracking()
                .Where(item => item.ProposalId == proposalId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new ProposalShareLinkModel
                {
                    Id = item.Id,
                    ProposalId = item.ProposalId,
                    Token = item.Token,
                    PublicUrl = string.Empty,
                    ExpiresAt = item.ExpiresAt,
                    RevokedAt = item.RevokedAt,
                    IsActive = !item.RevokedAt.HasValue && (!item.ExpiresAt.HasValue || item.ExpiresAt > now),
                    CreatedByUserName = item.CreatedByUserName,
                    CreatedAt = item.CreatedAt,
                    LastViewedAt = item.LastViewedAt,
                    ViewCount = item.ViewCount
                })
                .ToArrayAsync(cancellationToken);
        }

        public async Task<ProposalShareLinkModel> CreateShareLink(long proposalId, CreateProposalShareLinkRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureProposalCanBeShared(proposalId, cancellationToken);

            string token = GenerateToken();
            DateTimeOffset? expiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(30);
            ProposalShareLink shareLink = new(proposalId, token, expiresAt, currentUser.UserId, currentUser.UserName);

            dbContext.Set<ProposalShareLink>().Add(shareLink);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(shareLink, DateTimeOffset.UtcNow);
        }

        public async Task<ProposalShareLinkModel> RevokeShareLink(long proposalId, long shareLinkId, CancellationToken cancellationToken = default)
        {
            ProposalShareLink? shareLink = await dbContext.Set<ProposalShareLink>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == shareLinkId && item.ProposalId == proposalId, cancellationToken);

            if (shareLink is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            shareLink.Revoke();
            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(shareLink, DateTimeOffset.UtcNow);
        }

        private async Task EnsureProposalExists(long proposalId, CancellationToken cancellationToken)
        {
            bool exists = await dbContext.Set<Proposal>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == proposalId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }

        private async Task EnsureProposalCanBeShared(long proposalId, CancellationToken cancellationToken)
        {
            ProposalStatus? status = await dbContext.Set<Proposal>()
                .AsNoTracking()
                .Where(item => item.Id == proposalId)
                .Select(item => (ProposalStatus?)item.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (status is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            if (status == ProposalStatus.Draft)
            {
                throw new InvalidOperationException("proposal.share.draftNotAllowed");
            }
        }

        private static string GenerateToken()
        {
            byte[] bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(24);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", string.Empty);
        }

        private static ProposalShareLinkModel Map(ProposalShareLink link, DateTimeOffset now)
        {
            return new ProposalShareLinkModel
            {
                Id = link.Id,
                ProposalId = link.ProposalId,
                Token = link.Token,
                PublicUrl = string.Empty,
                ExpiresAt = link.ExpiresAt,
                RevokedAt = link.RevokedAt,
                IsActive = link.IsActive(now),
                CreatedByUserName = link.CreatedByUserName,
                CreatedAt = link.CreatedAt,
                LastViewedAt = link.LastViewedAt,
                ViewCount = link.ViewCount
            };
        }
    }
}
