using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPayments
{
    public sealed class SchedulePaymentBatchRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        [Required]
        [MinLength(1)]
        public List<long> CreatorPaymentIds { get; set; } = [];

        public DateTimeOffset? ScheduledFor { get; set; }
    }
}
