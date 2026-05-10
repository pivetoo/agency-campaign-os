using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Creators
{
    public sealed class CreatorContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? StageName { get; init; }

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public string? Document { get; init; }

        public string? PixKey { get; init; }

        public PixKeyType? PixKeyType { get; init; }

        public string? PrimaryNiche { get; init; }

        public string? City { get; init; }

        public string? State { get; init; }

        public string? Notes { get; init; }

        public decimal DefaultAgencyFeePercent { get; init; }

        public string? PhotoUrl { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Creator, CreatorContract>> Projection => item => new CreatorContract
        {
            Id = item.Id,
            Name = item.Name,
            StageName = item.StageName,
            Email = item.Email,
            Phone = item.Phone,
            Document = item.Document,
            PixKey = item.PixKey,
            PixKeyType = item.PixKeyType,
            PrimaryNiche = item.PrimaryNiche,
            City = item.City,
            State = item.State,
            Notes = item.Notes,
            DefaultAgencyFeePercent = item.DefaultAgencyFeePercent,
            PhotoUrl = item.PhotoUrl,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
