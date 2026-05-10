using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CommercialPipelineStageTests
    {
        [Test]
        public void Constructor_should_force_final_behavior_to_none_when_not_final()
        {
            CommercialPipelineStage stage = new("Qualificação", 1, "#fff", isFinal: false, finalBehavior: CommercialPipelineStageFinalBehavior.Won);

            stage.FinalBehavior.Should().Be(CommercialPipelineStageFinalBehavior.None);
        }

        [Test]
        public void Constructor_should_keep_final_behavior_when_final()
        {
            CommercialPipelineStage stage = new("Ganha", 9, "#22c55e", isFinal: true, finalBehavior: CommercialPipelineStageFinalBehavior.Won);

            stage.FinalBehavior.Should().Be(CommercialPipelineStageFinalBehavior.Won);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Constructor_should_normalize_non_positive_sla_to_null(int sla)
        {
            CommercialPipelineStage stage = new("x", 1, "#fff", slaInDays: sla);

            stage.SlaInDays.Should().BeNull();
        }

        [TestCase(-0.1)]
        [TestCase(100.1)]
        public void Constructor_should_reject_default_probability_out_of_range(decimal probability)
        {
            Action act = () => _ = new CommercialPipelineStage("x", 1, "#fff", defaultProbability: probability);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase(0)]
        [TestCase(50)]
        [TestCase(100)]
        public void Constructor_should_accept_default_probability_within_range(decimal probability)
        {
            CommercialPipelineStage stage = new("x", 1, "#fff", defaultProbability: probability);

            stage.DefaultProbability.Should().Be(probability);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_should_reject_blank_name(string value)
        {
            Action act = () => _ = new CommercialPipelineStage(value, 1, "#fff");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_replace_state_and_touch_updatedAt()
        {
            CommercialPipelineStage stage = new("x", 1, "#fff");
            DateTimeOffset before = stage.UpdatedAt!.Value;
            Thread.Sleep(2);

            stage.Update("Ganha", 9, "#22c55e", "desc", false, true, CommercialPipelineStageFinalBehavior.Won, true, 80m, 5);

            stage.Name.Should().Be("Ganha");
            stage.IsFinal.Should().BeTrue();
            stage.FinalBehavior.Should().Be(CommercialPipelineStageFinalBehavior.Won);
            stage.DefaultProbability.Should().Be(80m);
            stage.SlaInDays.Should().Be(5);
            stage.UpdatedAt!.Value.Should().BeAfter(before);
        }
    }
}
