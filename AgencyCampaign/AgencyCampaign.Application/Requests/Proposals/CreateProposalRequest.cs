using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class CreateProposalRequest
    {
        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTimeOffset? ValidityUntil { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long OpportunityId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public long? CommercialResponsibleId { get; set; }
    }
}
