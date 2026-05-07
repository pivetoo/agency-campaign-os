namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialUserModel
    {
        public long Id { get; init; }

        public string Username { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? AvatarUrl { get; init; }

        public bool IsActive { get; init; }
    }
}
