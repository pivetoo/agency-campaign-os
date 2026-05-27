using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Opportunity : Entity
    {
        private readonly List<OpportunityFollowUp> followUps = [];
        private readonly List<Proposal> proposals = [];
        private readonly List<OpportunityStageHistory> stageHistory = [];
        private readonly List<OpportunityComment> comments = [];
        private readonly List<OpportunityTagAssignment> tagAssignments = [];

        public long BrandId { get; private set; }

        public Brand? Brand { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public long CommercialPipelineStageId { get; private set; }

        public CommercialPipelineStage? CommercialPipelineStage { get; private set; }

        public decimal EstimatedValue { get; private set; }

        public decimal Probability { get; private set; }

        public bool ProbabilityIsManual { get; private set; }

        public DateTimeOffset? ExpectedCloseAt { get; private set; }

        public long? ResponsibleUserId { get; private set; }

        public string? ResponsibleUserName { get; private set; }

        public string? ContactName { get; private set; }

        public string? ContactEmail { get; private set; }

        public string? ContactPhone { get; private set; }

        public long? OpportunitySourceId { get; private set; }

        public OpportunitySource? OpportunitySource { get; private set; }

        public string? Notes { get; private set; }

        public DateTimeOffset? ClosedAt { get; private set; }

        public string? LossReason { get; private set; }

        public long? LossReasonId { get; private set; }

        public string? WonNotes { get; private set; }

        public long? WinReasonId { get; private set; }

        public IReadOnlyCollection<OpportunityFollowUp> FollowUps => followUps.AsReadOnly();

        public IReadOnlyCollection<Proposal> Proposals => proposals.AsReadOnly();

        public IReadOnlyCollection<OpportunityStageHistory> StageHistory => stageHistory.AsReadOnly();

        public IReadOnlyCollection<OpportunityComment> Comments => comments.AsReadOnly();

        public IReadOnlyCollection<OpportunityTagAssignment> TagAssignments => tagAssignments.AsReadOnly();

        private Opportunity()
        {
        }

        public Opportunity(long brandId, long commercialPipelineStageId, string name, decimal estimatedValue, DateTimeOffset? expectedCloseAt = null, string? description = null, long? responsibleUserId = null, string? responsibleUserName = null, string? contactName = null, string? contactEmail = null, string? notes = null, long? createdByUserId = null, string? createdByUserName = null, string? contactPhone = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commercialPipelineStageId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(estimatedValue);

            BrandId = brandId;
            CommercialPipelineStageId = commercialPipelineStageId;
            Name = name.Trim();
            Description = Normalize(description);
            EstimatedValue = estimatedValue;
            ExpectedCloseAt = expectedCloseAt?.ToUniversalTime();
            ResponsibleUserId = responsibleUserId;
            ResponsibleUserName = Normalize(responsibleUserName);
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            ContactPhone = Normalize(contactPhone);
            Notes = Normalize(notes);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;

            stageHistory.Add(new OpportunityStageHistory(
                Id, null, commercialPipelineStageId, createdByUserId, createdByUserName, "Oportunidade criada"));
        }

        public void Update(long brandId, string name, decimal estimatedValue, DateTimeOffset? expectedCloseAt, string? description, long? responsibleUserId, string? responsibleUserName, string? contactName, string? contactEmail, string? notes, string? contactPhone = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(estimatedValue);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            EstimatedValue = estimatedValue;
            ExpectedCloseAt = expectedCloseAt?.ToUniversalTime();
            ResponsibleUserId = responsibleUserId;
            ResponsibleUserName = Normalize(responsibleUserName);
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            ContactPhone = Normalize(contactPhone);
            Notes = Normalize(notes);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ChangeStage(CommercialPipelineStage stage, long? changedByUserId = null, string? changedByUserName = null, string? reason = null)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (!stage.IsActive)
            {
                throw new InvalidOperationException("opportunity.stage.inactive");
            }

            long? fromStageId = CommercialPipelineStageId == 0 ? null : CommercialPipelineStageId;
            if (fromStageId == stage.Id)
            {
                return;
            }

            CommercialPipelineStageId = stage.Id;
            ApplyStageFinalBehavior(stage, null);
            ApplyStageProbability(stage);
            UpdatedAt = DateTimeOffset.UtcNow;

            stageHistory.Add(new OpportunityStageHistory(
                Id, fromStageId, stage.Id, changedByUserId, changedByUserName, reason));
        }

        public void SetSource(long? opportunitySourceId)
        {
            OpportunitySourceId = opportunitySourceId;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ReplaceTags(IEnumerable<long> tagIds)
        {
            ArgumentNullException.ThrowIfNull(tagIds);

            HashSet<long> desired = tagIds.Distinct().ToHashSet();
            tagAssignments.RemoveAll(item => !desired.Contains(item.OpportunityTagId));

            HashSet<long> current = tagAssignments.Select(item => item.OpportunityTagId).ToHashSet();
            foreach (long tagId in desired)
            {
                if (current.Contains(tagId))
                {
                    continue;
                }

                tagAssignments.Add(new OpportunityTagAssignment(Id, tagId));
            }

            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void SetProbability(decimal probability)
        {
            if (probability < 0m || probability > 100m)
            {
                throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0 and 100.");
            }

            Probability = probability;
            ProbabilityIsManual = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ResetProbabilityToStageDefault(CommercialPipelineStage stage)
        {
            ArgumentNullException.ThrowIfNull(stage);

            ProbabilityIsManual = false;
            ApplyStageProbability(stage);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private void ApplyStageProbability(CommercialPipelineStage stage)
        {
            if (stage.FinalBehavior == CommercialPipelineStageFinalBehavior.Won)
            {
                Probability = 100m;
                return;
            }

            if (stage.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost)
            {
                Probability = 0m;
                return;
            }

            if (ProbabilityIsManual)
            {
                return;
            }

            if (stage.DefaultProbability.HasValue)
            {
                Probability = stage.DefaultProbability.Value;
            }
        }

        public void CloseAsWon(CommercialPipelineStage stage, string? wonNotes, long? winReasonId = null, long? changedByUserId = null, string? changedByUserName = null)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (IsClosed())
            {
                throw new InvalidOperationException("opportunity.alreadyClosed");
            }

            long? fromStageId = CommercialPipelineStageId == 0 ? null : CommercialPipelineStageId;

            CommercialPipelineStageId = stage.Id;
            WonNotes = Normalize(wonNotes);
            WinReasonId = winReasonId;
            LossReason = null;
            LossReasonId = null;
            ClosedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;

            stageHistory.Add(new OpportunityStageHistory(
                Id, fromStageId, stage.Id, changedByUserId, changedByUserName, WonNotes ?? "Fechada como ganha"));
        }

        public void CloseAsLost(CommercialPipelineStage stage, string lossReason, long? lossReasonId = null, long? changedByUserId = null, string? changedByUserName = null)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (IsClosed())
            {
                throw new InvalidOperationException("opportunity.alreadyClosed");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(lossReason);

            long? fromStageId = CommercialPipelineStageId == 0 ? null : CommercialPipelineStageId;

            CommercialPipelineStageId = stage.Id;
            LossReason = lossReason.Trim();
            LossReasonId = lossReasonId;
            WonNotes = null;
            WinReasonId = null;
            ClosedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;

            stageHistory.Add(new OpportunityStageHistory(
                Id, fromStageId, stage.Id, changedByUserId, changedByUserName, LossReason));
        }

        private bool IsClosed()
        {
            return ClosedAt.HasValue;
        }

        private void ApplyStageFinalBehavior(CommercialPipelineStage stage, string? notes)
        {
            if (stage.FinalBehavior == CommercialPipelineStageFinalBehavior.Won)
            {
                WonNotes = Normalize(notes) ?? "Oportunidade marcada como ganha pelo pipeline comercial.";
                LossReason = null;
                ClosedAt = DateTimeOffset.UtcNow;
                return;
            }

            if (stage.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost)
            {
                WonNotes = null;
                LossReason = Normalize(notes) ?? "Oportunidade marcada como perdida pelo pipeline comercial.";
                ClosedAt = DateTimeOffset.UtcNow;
                return;
            }

            WonNotes = null;
            LossReason = null;
            ClosedAt = null;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
