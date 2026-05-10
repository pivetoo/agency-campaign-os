using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class AutomationTests
    {
        [Test]
        public void Constructor_should_serialize_variable_mapping_to_json()
        {
            Dictionary<string, string> mapping = new() { ["recipient"] = "{{contactEmail}}" };

            Automation subject = new("Notify", AutomationTriggers.ProposalSent, 1, 2, variableMapping: mapping);

            subject.VariableMappingJson.Should().Contain("recipient");
            subject.GetVariableMapping().Should().ContainKey("recipient").WhoseValue.Should().Be("{{contactEmail}}");
        }

        [Test]
        public void Constructor_should_default_to_empty_json_when_no_mapping_provided()
        {
            Automation subject = new("x", AutomationTriggers.ProposalSent, 1, 2);

            subject.VariableMappingJson.Should().Be("{}");
            subject.GetVariableMapping().Should().BeEmpty();
        }

        [Test]
        public void Constructor_should_reject_blank_name_or_trigger()
        {
            Action blankName = () => _ = new Automation(" ", AutomationTriggers.ProposalSent, 1, 2);
            Action blankTrigger = () => _ = new Automation("x", "  ", 1, 2);

            blankName.Should().Throw<ArgumentException>();
            blankTrigger.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_replace_state()
        {
            Automation subject = new("Old", AutomationTriggers.ProposalSent, 1, 2);

            subject.Update("New", AutomationTriggers.ProposalApproved, 10, 20,
                triggerCondition: "amount > 1000",
                variableMapping: new Dictionary<string, string> { ["k"] = "v" },
                isActive: false);

            subject.Name.Should().Be("New");
            subject.Trigger.Should().Be(AutomationTriggers.ProposalApproved);
            subject.ConnectorId.Should().Be(10);
            subject.PipelineId.Should().Be(20);
            subject.TriggerCondition.Should().Be("amount > 1000");
            subject.GetVariableMapping().Should().ContainKey("k");
            subject.IsActive.Should().BeFalse();
        }

        [Test]
        public void Update_should_preserve_existing_mapping_when_null_passed()
        {
            Dictionary<string, string> initial = new() { ["recipient"] = "{{email}}" };
            Automation subject = new("x", AutomationTriggers.ProposalSent, 1, 2, variableMapping: initial);

            subject.Update("x", AutomationTriggers.ProposalSent, 1, 2, variableMapping: null);

            subject.GetVariableMapping().Should().ContainKey("recipient");
        }

        [Test]
        public void Update_should_preserve_active_state_when_null_passed()
        {
            Automation subject = new("x", AutomationTriggers.ProposalSent, 1, 2, isActive: false);

            subject.Update("x", AutomationTriggers.ProposalSent, 1, 2);

            subject.IsActive.Should().BeFalse();
        }
    }
}
