namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class ApprovalSummaryModel
    {
        public int Pending { get; init; }
        public int Approved { get; init; }
        public int Rejected { get; init; }
    }
}
