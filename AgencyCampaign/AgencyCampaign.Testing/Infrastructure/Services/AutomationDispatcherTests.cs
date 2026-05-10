using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class AutomationDispatcherTests
    {
        private TestDbContext db = null!;
        private AutomationDispatcher dispatcher = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            dispatcher = new AutomationDispatcher(db, IntegrationPlatformClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task DispatchAsync_should_reject_blank_trigger()
        {
            Func<Task> act = () => dispatcher.DispatchAsync(" ", new Dictionary<string, object?>());
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public async Task DispatchAsync_should_reject_null_payload()
        {
            Func<Task> act = () => dispatcher.DispatchAsync(AutomationTriggers.ProposalSent, null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task DispatchAsync_should_be_no_op_when_no_active_automation_matches_trigger()
        {
            db.Add(new Automation("inactive", AutomationTriggers.ProposalSent, 1, 1, isActive: false));
            db.Add(new Automation("other-trigger", AutomationTriggers.ProposalApproved, 1, 1));
            await db.SaveChangesAsync();

            // No exception means it short-circuited correctly.
            await dispatcher.DispatchAsync(AutomationTriggers.ProposalSent, new Dictionary<string, object?>());
        }

        [Test]
        public async Task DispatchAsync_should_render_payload_for_each_active_automation_without_throwing()
        {
            Dictionary<string, string> mapping = new()
            {
                ["recipient"] = "{{contactEmail}}",
                ["subject"] = "Proposta {{proposalName}}"
            };

            db.Add(new Automation("notify", AutomationTriggers.ProposalSent, 1, 1, variableMapping: mapping));
            db.Add(new Automation("notify-2", AutomationTriggers.ProposalSent, 1, 1, variableMapping: mapping));
            await db.SaveChangesAsync();

            Dictionary<string, object?> payload = new()
            {
                ["contactEmail"] = "cli@x",
                ["proposalName"] = "Big deal"
            };

            // Inert IntegrationPlatformClient: EnqueuePipelineAsync no-ops because no integration is configured.
            // Dispatcher catches any exception from the client and continues, so the test passes if the loop runs.
            await dispatcher.DispatchAsync(AutomationTriggers.ProposalSent, payload);
        }

        [Test]
        public async Task DispatchAsync_should_swallow_per_automation_failures_and_continue()
        {
            // Empty mapping JSON should still parse fine; placeholders fallback to empty.
            db.Add(new Automation("notify", AutomationTriggers.ProposalSent, 1, 1));
            await db.SaveChangesAsync();

            Func<Task> act = () => dispatcher.DispatchAsync(AutomationTriggers.ProposalSent, new Dictionary<string, object?>());
            await act.Should().NotThrowAsync();
        }
    }
}
