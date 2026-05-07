namespace AgencyCampaign.Application.Models.Creators
{
    public sealed class CreatorSummaryModel
    {
        public long CreatorId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public int TotalCampaigns { get; set; }
        public int ConfirmedCampaigns { get; set; }
        public int CancelledCampaigns { get; set; }
        public int TotalDeliverables { get; set; }
        public int PublishedDeliverables { get; set; }
        public int OverdueDeliverables { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalCreatorAmount { get; set; }
        public decimal TotalAgencyFeeAmount { get; set; }
        public decimal OnTimeDeliveryRate { get; set; }
        public IReadOnlyCollection<CreatorPerformanceByPlatformModel> PerformanceByPlatform { get; set; } = [];
    }

    public sealed class CreatorPerformanceByPlatformModel
    {
        public long PlatformId { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public int Deliverables { get; set; }
        public int Published { get; set; }
        public decimal GrossAmount { get; set; }
    }
}
