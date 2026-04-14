using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.DeliverableKinds
{
    public sealed class CreateDeliverableKindRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}
