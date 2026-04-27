using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.IntegrationPipelines
{
    public sealed class IntegrationPipelineContract
    {
        public long Id { get; init; }

        public string Identifier { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public long IntegrationId { get; init; }

        public string IntegrationName { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<IntegrationPipeline, IntegrationPipelineContract>> Projection => item => new IntegrationPipelineContract
        {
            Id = item.Id,
            Identifier = item.Identifier,
            Name = item.Name,
            Description = item.Description,
            IntegrationId = item.IntegrationId,
            IntegrationName = item.Integration != null ? item.Integration.Name : string.Empty,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
