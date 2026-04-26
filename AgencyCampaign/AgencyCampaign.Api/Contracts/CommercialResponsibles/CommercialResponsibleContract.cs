using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CommercialResponsibles
{
    public sealed class CommercialResponsibleContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public string? Notes { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CommercialResponsible, CommercialResponsibleContract>> Projection => item => new CommercialResponsibleContract
        {
            Id = item.Id,
            Name = item.Name,
            Email = item.Email,
            Phone = item.Phone,
            Notes = item.Notes,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
