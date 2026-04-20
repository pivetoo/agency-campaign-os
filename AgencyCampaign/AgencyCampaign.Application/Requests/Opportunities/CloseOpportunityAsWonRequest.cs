using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CloseOpportunityAsWonRequest
    {
        [StringLength(1000)]
        public string? WonNotes { get; set; }
    }
}
