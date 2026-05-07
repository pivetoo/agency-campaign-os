using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.AgencySettings
{
    public sealed class UpdateAgencySettingsRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string AgencyName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? TradeName { get; set; }

        [StringLength(50)]
        public string? Document { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? PrimaryEmail { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(32)]
        public string? PrimaryColor { get; set; }

        public long? DefaultEmailConnectorId { get; set; }

        public long? DefaultEmailPipelineId { get; set; }
    }
}
