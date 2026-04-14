using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.DeliverableKinds
{
    public sealed class UpdateDeliverableKindRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
