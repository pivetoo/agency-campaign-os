namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class RecordReviewerDecisionRequest
    {
        public bool Approved { get; set; }

        public string? Notes { get; set; }
    }
}
