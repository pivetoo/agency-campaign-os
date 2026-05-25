namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class PolicyEvaluationModel
    {
        public bool HasDeviations { get; init; }

        public bool PolicyMissing { get; init; }

        public string? SuggestedApprovalType { get; init; }

        public IReadOnlyCollection<PolicyDeviationModel> Deviations { get; init; } = Array.Empty<PolicyDeviationModel>();

        public IReadOnlyCollection<PolicyImpactModel> Impacts { get; init; } = Array.Empty<PolicyImpactModel>();
    }

    public sealed class PolicyDeviationModel
    {
        public string Field { get; init; } = string.Empty;

        public string PolicyValue { get; init; } = string.Empty;

        public string RequestedValue { get; init; } = string.Empty;

        public string Delta { get; init; } = string.Empty;

        public int Kind { get; init; }

        public bool IsViolation { get; init; }
    }

    public sealed class PolicyImpactModel
    {
        public string Label { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;

        public bool IsGood { get; init; }
    }
}
