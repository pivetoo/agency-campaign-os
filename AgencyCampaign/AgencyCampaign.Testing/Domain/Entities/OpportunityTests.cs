using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class OpportunityTests
    {
        private static Opportunity BuildDefault(long stageId = 5)
        {
            return new Opportunity(
                brandId: 1,
                commercialPipelineStageId: stageId,
                name: "  Big deal  ",
                estimatedValue: 10000m,
                description: "  desc  ");
        }

        [Test]
        public void Constructor_should_register_initial_stage_history()
        {
            Opportunity subject = BuildDefault(stageId: 7);

            subject.Name.Should().Be("Big deal");
            subject.Description.Should().Be("desc");
            subject.StageHistory.Should().HaveCount(1);
            subject.StageHistory.Single().FromStageId.Should().BeNull();
            subject.StageHistory.Single().ToStageId.Should().Be(7);
        }

        [Test]
        public void Constructor_should_reject_invalid_inputs()
        {
            Action negativeBrand = () => _ = new Opportunity(0, 1, "x", 0);
            Action blankName = () => _ = new Opportunity(1, 1, " ", 0);
            Action negativeValue = () => _ = new Opportunity(1, 1, "x", -1m);

            negativeBrand.Should().Throw<ArgumentOutOfRangeException>();
            blankName.Should().Throw<ArgumentException>();
            negativeValue.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void SetProbability_should_mark_probability_as_manual()
        {
            Opportunity subject = BuildDefault();

            subject.SetProbability(70m);

            subject.Probability.Should().Be(70m);
            subject.ProbabilityIsManual.Should().BeTrue();
        }

        [TestCase(-0.1)]
        [TestCase(100.1)]
        public void SetProbability_should_reject_out_of_range_values(decimal value)
        {
            Opportunity subject = BuildDefault();
            Action act = () => subject.SetProbability(value);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ChangeStage_should_reject_inactive_stage()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage stage = new CommercialPipelineStageBuilder().Inactive().WithId(99).Build();

            Action act = () => subject.ChangeStage(stage);
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void ChangeStage_should_be_no_op_when_target_stage_is_current()
        {
            Opportunity subject = BuildDefault(stageId: 5);
            CommercialPipelineStage same = new CommercialPipelineStageBuilder().WithId(5).Build();
            int historyBefore = subject.StageHistory.Count;

            subject.ChangeStage(same);

            subject.StageHistory.Count.Should().Be(historyBefore);
        }

        [Test]
        public void ChangeStage_to_won_stage_should_close_opportunity_and_set_probability_to_100()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage wonStage = new CommercialPipelineStageBuilder()
                .WithId(20)
                .AsFinal(CommercialPipelineStageFinalBehavior.Won)
                .Build();

            subject.ChangeStage(wonStage);

            subject.Probability.Should().Be(100m);
            subject.ClosedAt.Should().NotBeNull();
            subject.WonNotes.Should().NotBeNullOrWhiteSpace();
            subject.LossReason.Should().BeNull();
        }

        [Test]
        public void ChangeStage_to_lost_stage_should_close_opportunity_and_set_probability_to_zero()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage lostStage = new CommercialPipelineStageBuilder()
                .WithId(21)
                .AsFinal(CommercialPipelineStageFinalBehavior.Lost)
                .Build();

            subject.ChangeStage(lostStage);

            subject.Probability.Should().Be(0m);
            subject.ClosedAt.Should().NotBeNull();
            subject.LossReason.Should().NotBeNullOrWhiteSpace();
            subject.WonNotes.Should().BeNull();
        }

        [Test]
        public void ChangeStage_should_apply_default_probability_when_not_manual()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage withProbability = new CommercialPipelineStageBuilder()
                .WithId(30)
                .WithDefaultProbability(40m)
                .Build();

            subject.ChangeStage(withProbability);

            subject.Probability.Should().Be(40m);
            subject.ProbabilityIsManual.Should().BeFalse();
        }

        [Test]
        public void ChangeStage_should_preserve_manual_probability_for_non_final_stages()
        {
            Opportunity subject = BuildDefault();
            subject.SetProbability(85m);

            CommercialPipelineStage withProbability = new CommercialPipelineStageBuilder()
                .WithId(31)
                .WithDefaultProbability(40m)
                .Build();

            subject.ChangeStage(withProbability);

            subject.Probability.Should().Be(85m);
        }

        [Test]
        public void CloseAsWon_should_set_won_notes_and_clear_loss_reason()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage wonStage = new CommercialPipelineStageBuilder().WithId(40).Build();

            subject.CloseAsWon(wonStage, "  fechou na call  ");

            subject.WonNotes.Should().Be("fechou na call");
            subject.LossReason.Should().BeNull();
            subject.ClosedAt.Should().NotBeNull();
        }

        [Test]
        public void CloseAsWon_should_throw_when_already_closed()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage stage = new CommercialPipelineStageBuilder().WithId(40).Build();
            subject.CloseAsWon(stage, "ok");

            Action act = () => subject.CloseAsWon(stage, "again");
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void CloseAsLost_should_require_loss_reason()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage stage = new CommercialPipelineStageBuilder().WithId(41).Build();

            Action act = () => subject.CloseAsLost(stage, "   ");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void CloseAsLost_should_set_loss_reason_and_clear_won_notes()
        {
            Opportunity subject = BuildDefault();
            CommercialPipelineStage stage = new CommercialPipelineStageBuilder().WithId(41).Build();

            subject.CloseAsLost(stage, "  preço  ");

            subject.LossReason.Should().Be("preço");
            subject.WonNotes.Should().BeNull();
            subject.ClosedAt.Should().NotBeNull();
        }

        [Test]
        public void ReplaceTags_should_add_missing_tags_and_remove_extras()
        {
            Opportunity subject = BuildDefault().WithId(1);
            subject.ReplaceTags(new long[] { 1, 2, 3 });
            subject.TagAssignments.Should().HaveCount(3);

            subject.ReplaceTags(new long[] { 2, 4 });

            subject.TagAssignments.Select(item => item.OpportunityTagId).Should().BeEquivalentTo(new long[] { 2, 4 });
        }

        [Test]
        public void ReplaceTags_should_dedupe_input()
        {
            Opportunity subject = BuildDefault().WithId(1);

            subject.ReplaceTags(new long[] { 1, 1, 2, 2, 3 });

            subject.TagAssignments.Select(item => item.OpportunityTagId).Should().BeEquivalentTo(new long[] { 1, 2, 3 });
        }

        [Test]
        public void SetSource_should_persist_id()
        {
            Opportunity subject = BuildDefault();

            subject.SetSource(42);

            subject.OpportunitySourceId.Should().Be(42);
        }
    }
}
