namespace AgencyCampaign.Infrastructure.Options
{
    public sealed class WebhookOptions
    {
        public string ProviderCallbackSecret { get; set; } = string.Empty;
    }
}
