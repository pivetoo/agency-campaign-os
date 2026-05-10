using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignCreatorStatusTests
    {
        [Test]
        public void Constructor_should_initialize_with_provided_metadata()
        {
            CampaignCreatorStatus status = new("  Confirmado  ", 1, "  #22c55e  ",
                description: "  ok  ",
                isInitial: true,
                isFinal: false,
                category: CampaignCreatorStatusCategory.Success,
                marksAsConfirmed: true);

            status.Name.Should().Be("Confirmado");
            status.Color.Should().Be("#22c55e");
            status.Description.Should().Be("ok");
            status.IsInitial.Should().BeTrue();
            status.Category.Should().Be(CampaignCreatorStatusCategory.Success);
            status.MarksAsConfirmed.Should().BeTrue();
            status.IsActive.Should().BeTrue();
        }

        [Test]
        public void MarksAsCancelled_should_be_true_only_when_category_is_failure()
        {
            CampaignCreatorStatus failure = new("Cancelado", 9, "#ef4444", category: CampaignCreatorStatusCategory.Failure);
            CampaignCreatorStatus success = new("Confirmado", 1, "#22c55e", category: CampaignCreatorStatusCategory.Success);

            failure.MarksAsCancelled.Should().BeTrue();
            success.MarksAsCancelled.Should().BeFalse();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_should_reject_blank_name(string value)
        {
            Action act = () => _ = new CampaignCreatorStatus(value, 1, "#fff");
            act.Should().Throw<ArgumentException>();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_should_reject_blank_color(string value)
        {
            Action act = () => _ = new CampaignCreatorStatus("x", 1, value);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_replace_state()
        {
            CampaignCreatorStatus status = new("x", 1, "#fff");

            status.Update("y", 2, "#000", "d", false, true, CampaignCreatorStatusCategory.Failure, false, false);

            status.Name.Should().Be("y");
            status.IsFinal.Should().BeTrue();
            status.Category.Should().Be(CampaignCreatorStatusCategory.Failure);
            status.IsActive.Should().BeFalse();
            status.MarksAsCancelled.Should().BeTrue();
        }
    }
}
