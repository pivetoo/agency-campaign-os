using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.IntegrationLogs
{
    public sealed class IntegrationLogContract
    {
        public long Id { get; init; }

        public long IntegrationPipelineId { get; init; }

        public string IntegrationPipelineName { get; init; } = string.Empty;

        public string IntegrationName { get; init; } = string.Empty;

        public int Status { get; init; }

        public string? Payload { get; init; }

        public string? Response { get; init; }

        public long? DurationMs { get; init; }

        public string? ErrorMessage { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<IntegrationLog, IntegrationLogContract>> Projection => item => new IntegrationLogContract
        {
            Id = item.Id,
            IntegrationPipelineId = item.IntegrationPipelineId,
            IntegrationPipelineName = item.IntegrationPipeline != null ? item.IntegrationPipeline.Name : string.Empty,
            IntegrationName = item.IntegrationPipeline != null && item.IntegrationPipeline.Integration != null ? item.IntegrationPipeline.Integration.Name : string.Empty,
            Status = item.Status,
            Payload = item.Payload,
            Response = item.Response,
            DurationMs = item.DurationMs,
            ErrorMessage = item.ErrorMessage,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
