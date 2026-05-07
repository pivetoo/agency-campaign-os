using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalShareLinkService : IProposalShareLinkService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalShareLinkService(DbContext dbContext, ICurrentUser currentUser, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.localizer = localizer;
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
            await EnsureProposalExists(proposalId, cancellationToken);

            string token = GenerateToken();
            ProposalShareLink shareLink = new(proposalId, token, request.ExpiresAt, currentUser.UserId, currentUser.UserName);

            dbContext.Set<ProposalShareLink>().Add(shareLink);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(shareLink, DateTimeOffset.UtcNow);
        }

        public async Task<ProposalShareLinkModel> RevokeShareLink(long shareLinkId, CancellationToken cancellationToken = default)
        {
            ProposalShareLink? shareLink = await dbContext.Set<ProposalShareLink>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == shareLinkId, cancellationToken);

            if (shareLink is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
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
                throw new InvalidOperationException(localizer["record.notFound"]);
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
