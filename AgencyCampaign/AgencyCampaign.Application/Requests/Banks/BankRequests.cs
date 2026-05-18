using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Banks
{
    public class CreateBankRequest
    {
        [Required]
        [RegularExpression("^[0-9]{3}$", ErrorMessage = "validation.bank.compe.invalidFormat")]
        public string Compe { get; set; } = string.Empty;

        [StringLength(8)]
        [RegularExpression("^[0-9]{0,8}$", ErrorMessage = "validation.bank.ispb.invalidFormat")]
        public string? Ispb { get; set; }

        [Required]
        [StringLength(160, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(80, MinimumLength = 2)]
        public string ShortName { get; set; } = string.Empty;
    }

    public sealed class UpdateBankRequest : CreateBankRequest
    {
        [Required]
        public long Id { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
