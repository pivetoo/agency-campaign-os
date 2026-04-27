namespace AgencyCampaign.Application.Requests.CampaignCreatorStatuses
{
    public sealed class UpdateCampaignCreatorStatusRequest : CreateCampaignCreatorStatusRequest
    {
        public long Id { get; set; }

        public bool IsActive { get; set; }
    }
}
