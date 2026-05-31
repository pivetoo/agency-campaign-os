using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class ConvertToNewCampaignRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        public DateTimeOffset? StartDate { get; set; }
    }
}
