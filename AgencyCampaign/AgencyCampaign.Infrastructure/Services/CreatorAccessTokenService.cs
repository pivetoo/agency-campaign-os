using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorAccessTokens;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Application.MultiTenancy;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Cryptography;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorAccessTokenService : CrudService<CreatorAccessToken>, ICreatorAccessTokenService
    {
        private const int DefaultTokenLifetimeDays = 30;
        private const int MaxTokenLifetimeDays = 90;

        private readonly ICurrentUser currentUser;
        private readonly ITenantContext tenantContext;

        public CreatorAccessTokenService(DbContext dbContext, ICurrentUser currentUser, ITenantContext tenantContext) : base(dbContext)
        {
            this.currentUser = currentUser;
            this.tenantContext = tenantContext;
        }

        public async Task<CreatorAccessToken> Issue(IssueCreatorAccessTokenRequest request, CancellationToken cancellationToken = default)
        {
            bool creatorExists = await DbContext.Set<Creator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == request.CreatorId, cancellationToken);

            if (!creatorExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            string tokenValue = PublicLinkToken.Compose(tenantContext.TenantId, GenerateToken());
            CreatorAccessToken token = new(
                request.CreatorId,
                tokenValue,
                ResolveExpiry(request.ExpiresAt),
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

        private static string GenerateToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static DateTimeOffset ResolveExpiry(DateTimeOffset? requested)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset maxExpiry = now.AddDays(MaxTokenLifetimeDays);
            DateTimeOffset expiresAt = requested?.ToUniversalTime() ?? now.AddDays(DefaultTokenLifetimeDays);
            if (expiresAt <= now)
            {
                expiresAt = now.AddDays(DefaultTokenLifetimeDays);
            }
            if (expiresAt > maxExpiry)
            {
                expiresAt = maxExpiry;
            }
            return expiresAt;
        }
    }
}
