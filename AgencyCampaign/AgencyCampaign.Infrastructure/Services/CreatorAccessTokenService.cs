using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorAccessTokens;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorAccessTokenService : CrudService<CreatorAccessToken>, ICreatorAccessTokenService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly ICurrentUser currentUser;

        public CreatorAccessTokenService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, ICurrentUser currentUser) : base(dbContext)
        {
            this.localizer = localizer;
            this.currentUser = currentUser;
        }

        public async Task<CreatorAccessToken> Issue(IssueCreatorAccessTokenRequest request, CancellationToken cancellationToken = default)
        {
            bool creatorExists = await DbContext.Set<Creator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == request.CreatorId, cancellationToken);

            if (!creatorExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            string tokenValue = Guid.NewGuid().ToString("N");
            CreatorAccessToken token = new(
                request.CreatorId,
                tokenValue,
                request.ExpiresAt,
                request.Note,
                currentUser.UserId,
                currentUser.UserName);

            bool success = await Insert(cancellationToken, token);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return token;
        }

        public async Task<List<CreatorAccessToken>> GetByCreator(long creatorId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CreatorAccessToken>()
                .AsNoTracking()
                .Where(item => item.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<CreatorAccessToken?> ValidateToken(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            CreatorAccessToken? entity = await DbContext.Set<CreatorAccessToken>()
                .AsTracking()
                .Include(item => item.Creator)
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (entity is null || !entity.IsValid(DateTimeOffset.UtcNow))
            {
                return null;
            }

            entity.RegisterUse();
            await DbContext.SaveChangesAsync(cancellationToken);

            return entity;
        }

        public async Task<bool> Revoke(long id, CancellationToken cancellationToken = default)
        {
            CreatorAccessToken? token = await DbContext.Set<CreatorAccessToken>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (token is null)
            {
                return false;
            }

            token.Revoke();
            await DbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
