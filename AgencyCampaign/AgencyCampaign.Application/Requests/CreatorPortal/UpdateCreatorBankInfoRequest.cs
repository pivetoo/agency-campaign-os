using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPortal
{
    public sealed class UpdateCreatorBankInfoRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 1)]
        public string PixKey { get; set; } = string.Empty;

        [Required]
        public PixKeyType PixKeyType { get; set; }

        [StringLength(30)]
        public string? Document { get; set; }
    }
}
