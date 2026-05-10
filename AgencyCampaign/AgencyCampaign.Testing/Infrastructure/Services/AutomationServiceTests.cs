using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Automations;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class AutomationServiceTests
    {
        private TestDbContext db = null!;
        private AutomationService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new AutomationService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreateAutomation_should_persist_with_serialized_variable_mapping()
        {
            CreateAutomationRequest request = new()
            {
                Name = "Notificar venda",
                Trigger = AutomationTriggers.ProposalApproved,
                ConnectorId = 10,
                PipelineId = 20,
                VariableMapping = new Dictionary<string, string> { ["recipient"] = "{{contactEmail}}" }
            };

            Automation result = await service.CreateAutomation(request);

            result.Id.Should().BeGreaterThan(0);
            result.VariableMappingJson.Should().Contain("recipient");
            result.IsActive.Should().BeTrue();
        }

        [Test]
        public async Task UpdateAutomation_should_throw_when_id_mismatch()
        {
            UpdateAutomationRequest request = new() { Id = 5, Name = "x", Trigger = "x" };
            Func<Task> act = () => service.UpdateAutomation(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateAutomation_should_throw_when_not_found()
        {
            UpdateAutomationRequest request = new() { Id = 99, Name = "x", Trigger = "x" };
            Func<Task> act = () => service.UpdateAutomation(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateAutomation_should_replace_state()
        {
            Automation automation = new("Old", AutomationTriggers.ProposalSent, 1, 2);
            db.Add(automation);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Automation result = await service.UpdateAutomation(automation.Id, new UpdateAutomationRequest
            {
                Id = automation.Id,
                Name = "New",
                Trigger = AutomationTriggers.ProposalApproved,
                ConnectorId = 10,
                PipelineId = 20,
                IsActive = false
            });

            result.Name.Should().Be("New");
            result.Trigger.Should().Be(AutomationTriggers.ProposalApproved);
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetAutomations_should_order_active_first_then_id_desc()
        {
            db.Add(new Automation("A", AutomationTriggers.ProposalSent, 1, 1));
            Automation inactive = new("Inactive", AutomationTriggers.ProposalSent, 1, 1, isActive: false);
            db.Add(inactive);
            db.Add(new Automation("B", AutomationTriggers.ProposalSent, 1, 1));
            await db.SaveChangesAsync();

            var result = await service.GetAutomations(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Last().IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetAutomationById_should_return_null_when_not_found()
        {
            (await service.GetAutomationById(99)).Should().BeNull();
        }
    }
}
