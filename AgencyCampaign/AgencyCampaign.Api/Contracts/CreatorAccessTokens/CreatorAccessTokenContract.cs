using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CreatorAccessTokens
{
    public sealed class CreatorAccessTokenContract
    {
        public long Id { get; init; }
        public long CreatorId { get; init; }
        public string Token { get; init; } = string.Empty;
        public DateTimeOffset? ExpiresAt { get; init; }
        public DateTimeOffset? RevokedAt { get; init; }
        public DateTimeOffset? LastUsedAt { get; init; }
        public int UsageCount { get; init; }
        public string? Note { get; init; }
        public string? CreatedByUserName { get; init; }
        public DateTimeOffset CreatedAt { get; init; }

        public static Expression<Func<CreatorAccessToken, CreatorAccessTokenContract>> Projection => item => new CreatorAccessTokenContract
        {
            Id = item.Id,
            CreatorId = item.CreatorId,
            Token = item.Token,
            ExpiresAt = item.ExpiresAt,
            RevokedAt = item.RevokedAt,
            LastUsedAt = item.LastUsedAt,
            UsageCount = item.UsageCount,
            Note = item.Note,
            CreatedByUserName = item.CreatedByUserName,
            CreatedAt = item.CreatedAt,
        };
    }
}
