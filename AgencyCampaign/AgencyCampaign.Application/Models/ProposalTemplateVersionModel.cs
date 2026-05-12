namespace AgencyCampaign.Application.Models
{
    public sealed class ProposalTemplateVersionModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
