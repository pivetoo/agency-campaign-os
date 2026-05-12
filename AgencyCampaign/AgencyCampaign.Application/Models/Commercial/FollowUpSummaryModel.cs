namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class FollowUpSummaryModel
    {
        public int Overdue { get; init; }
        public int Today { get; init; }
        public int Upcoming { get; init; }
        public int Completed { get; init; }
    }
}
