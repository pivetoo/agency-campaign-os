using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProposalTemplateServiceTests
    {
        private TestDbContext db = null!;
        private ProposalTemplateService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new ProposalTemplateService(db, CurrentUserMock.Create(), LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_persist_template_with_items()
        {
            ProposalTemplateModel result = await service.Create(new CreateProposalTemplateRequest
            {
                Name = "Pacote básico",
                Items = new[]
                {
                    new ProposalTemplateItemRequest { Description = "Story", DefaultQuantity = 2, DefaultUnitPrice = 500m, DisplayOrder = 1 },
                    new ProposalTemplateItemRequest { Description = "Reels", DefaultQuantity = 1, DefaultUnitPrice = 1500m, DisplayOrder = 2 }
                }
            });

            result.Items.Should().HaveCount(2);
            result.Items.First().DisplayOrder.Should().Be(1);
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateProposalTemplateRequest request = new() { Id = 5, Name = "x" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_replace_items()
        {
            ProposalTemplate template = new("Old", null, null, null);
            db.Add(template);
            await db.SaveChangesAsync();

            db.Add(new ProposalTemplateItem(template.Id, "old item", 1, 100m, null, null, 1));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            ProposalTemplateModel result = await service.Update(template.Id, new UpdateProposalTemplateRequest
            {
                Id = template.Id,
                Name = "New",
                Items = new[]
                {
                    new ProposalTemplateItemRequest { Description = "novo", DefaultQuantity = 3, DefaultUnitPrice = 200m, DisplayOrder = 1 }
                }
            });

            result.Items.Should().ContainSingle(item => item.Description == "novo");
        }

        [Test]
        public async Task Delete_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Delete(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ApplyToProposal_should_create_items_from_template()
        {
            Proposal proposal = new(opportunityId: 1, name: "P", internalOwnerId: 1);
            db.Add(proposal);
            ProposalTemplate template = new("Pacote", null, null, null);
            db.Add(template);
            await db.SaveChangesAsync();

            db.Add(new ProposalTemplateItem(template.Id, "item 1", 2, 100m, defaultDeliveryDays: 5, observations: null, displayOrder: 1));
            db.Add(new ProposalTemplateItem(template.Id, "item 2", 1, 200m, defaultDeliveryDays: null, observations: null, displayOrder: 2));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            int created = await service.ApplyToProposal(proposal.Id, template.Id);

            created.Should().Be(2);
            (await db.Set<ProposalItem>().CountAsync()).Should().Be(2);
        }

        [Test]
        public async Task ApplyToProposal_should_throw_when_proposal_not_found()
        {
            ProposalTemplate template = new("T", null, null, null);
            db.Add(template);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.ApplyToProposal(99, template.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ApplyToProposal_should_throw_when_template_inactive_or_not_found()
        {
            Proposal proposal = new(1, "P", 1);
            db.Add(proposal);
            ProposalTemplate inactive = new("T", null, null, null);
            inactive.Update("T", null, false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.ApplyToProposal(proposal.Id, inactive.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
