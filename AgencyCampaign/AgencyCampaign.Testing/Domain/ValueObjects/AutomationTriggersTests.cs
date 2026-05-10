using System.Reflection;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.ValueObjects
{
    [TestFixture]
    public sealed class AutomationTriggersTests
    {
        [Test]
        public void Labels_should_have_one_entry_for_every_trigger_constant()
        {
            string[] constants = typeof(AutomationTriggers)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToArray();

            constants.Should().NotBeEmpty();
            AutomationTriggers.Labels.Keys.Should().BeEquivalentTo(constants);
        }

        [Test]
        public void Labels_should_have_non_empty_portuguese_descriptions()
        {
            AutomationTriggers.Labels.Values.Should().OnlyContain(value => !string.IsNullOrWhiteSpace(value));
        }

        [Test]
        public void Trigger_constants_should_be_unique()
        {
            string[] constants = typeof(AutomationTriggers)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToArray();

            constants.Should().OnlyHaveUniqueItems();
        }
    }
}
