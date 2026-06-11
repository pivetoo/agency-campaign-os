using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProposalItemServiceTests
    {
        private TestDbContext db = null!;
        private ProposalItemService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new ProposalItemService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Proposal> SeedProposalAsync()
        {
            Proposal proposal = new(opportunityId: 1, name: "P", internalOwnerId: 1);
            db.Add(proposal);
            await db.SaveChangesAsync();
            return proposal;
        }

        [Test]
        public async Task CreateProposalItem_should_throw_when_proposal_not_found()
        {
            CreateProposalItemRequest request = new() { ProposalId = 99, Description = "x", Quantity = 1, UnitPrice = 100m };

            Func<Task> act = () => service.CreateProposalItem(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateProposalItem_should_throw_when_creator_not_found()
        {
            Proposal proposal = await SeedProposalAsync();

            CreateProposalItemRequest request = new()
            {
                ProposalId = proposal.Id,
                Description = "x",
                Quantity = 1,
                UnitPrice = 100m,
                CreatorId = 99
            };

            Func<Task> act = () => service.CreateProposalItem(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateProposalItem_should_block_when_proposal_is_not_draft()
        {
            Proposal proposal = await SeedProposalAsync();
            proposal.MarkAsSent();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.CreateProposalItem(new CreateProposalItemRequest
            {
                ProposalId = proposal.Id,
                Description = "Item tardio",
                Quantity = 1,
                UnitPrice = 100m
            });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("proposal.locked.notDraft");
        }

        [Test]
        public async Task CreateProposalItem_should_persist_and_recalculate_total()
        {
            Proposal proposal = await SeedProposalAsync();

            await service.CreateProposalItem(new CreateProposalItemRequest
            {
                ProposalId = proposal.Id,
                Description = "Item 1",
                Quantity = 2,
                UnitPrice = 100m
            });
            await service.CreateProposalItem(new CreateProposalItemRequest
            {
                ProposalId = proposal.Id,
                Description = "Item 2",
                Quantity = 1,
                UnitPrice = 50m
            });

            db.ChangeTracker.Clear();
            Proposal persisted = await db.Set<Proposal>().AsNoTracking().SingleAsync();
            persisted.TotalValue.Should().Be(250m);
        }

        [Test]
        public async Task CreateProposalItem_should_persist_commission_pricing_and_use_estimate_in_total()
        {
            Proposal proposal = await SeedProposalAsync();

            await service.CreateProposalItem(new CreateProposalItemRequest
            {
                ProposalId = proposal.Id,
                Description = "Comissao de vendas",
                Quantity = 1,
                UnitPrice = 0m,
                PricingModel = ProposalItemPricingModel.Commission,
                VariableRate = 10m,
                VariableBasis = 50000m
            });

            db.ChangeTracker.Clear();
            ProposalItem item = await db.Set<ProposalItem>().AsNoTracking().SingleAsync();
            item.PricingModel.Should().Be(ProposalItemPricingModel.Commission);
            item.VariableRate.Should().Be(10m);
            item.VariableBasis.Should().Be(50000m);
            item.Total.Should().Be(5000m);

            Proposal persisted = await db.Set<Proposal>().AsNoTracking().SingleAsync();
            persisted.TotalValue.Should().Be(5000m);
        }

        [Test]
        public async Task UpdateProposalItem_should_throw_when_not_found()
        {
            UpdateProposalItemRequest request = new() { Description = "x", Quantity = 1, UnitPrice = 100m };
            Func<Task> act = () => service.UpdateProposalItem(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task DeleteProposalItem_should_throw_when_not_found()
        {
            Func<Task> act = () => service.DeleteProposalItem(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetProposalItemById_should_return_null_when_not_found()
        {
            (await service.GetProposalItemById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetProposalItemById_should_return_item_when_found()
        {
            Proposal proposal = await SeedProposalAsync();
            ProposalItem item = new(proposal.Id, "x", 1, 100m);
            db.Add(item);
            await db.SaveChangesAsync();

            ProposalItem? result = await service.GetProposalItemById(item.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateProposalItem_should_persist_changes_and_recalculate_total()
        {
            Proposal proposal = await SeedProposalAsync();
            ProposalItem item = new(proposal.Id, "old", 1, 100m);
            db.Add(item);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateProposalItemRequest request = new()
            {
                Description = "new",
                Quantity = 2,
                UnitPrice = 250m
            };

            ProposalItem result = await service.UpdateProposalItem(item.Id, request);

            result.Description.Should().Be("new");
            result.Quantity.Should().Be(2);
            result.UnitPrice.Should().Be(250m);

            db.ChangeTracker.Clear();
            Proposal refreshed = await db.Set<Proposal>().AsNoTracking().SingleAsync();
            refreshed.TotalValue.Should().Be(500m);
        }

        [Test]
        public async Task DeleteProposalItem_should_remove_and_recalculate_total()
        {
            Proposal proposal = await SeedProposalAsync();
            ProposalItem keep = new(proposal.Id, "keep", 1, 100m);
            ProposalItem remove = new(proposal.Id, "remove", 1, 200m);
            db.Add(keep);
            db.Add(remove);
            await db.SaveChangesAsync();

            await service.DeleteProposalItem(remove.Id);

            db.ChangeTracker.Clear();
            Proposal refreshed = await db.Set<Proposal>().AsNoTracking().SingleAsync();
            refreshed.TotalValue.Should().Be(100m);
            (await db.Set<ProposalItem>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task GetItemsByProposalId_should_filter_by_proposal()
        {
            Proposal proposal = await SeedProposalAsync();
            db.Add(new ProposalItem(proposal.Id, "p1", 1, 100m).WithId(1));
            db.Add(new ProposalItem(proposal.Id, "p2", 1, 200m).WithId(2));
            db.Add(new ProposalItem(99, "outro", 1, 300m).WithId(3));
            await db.SaveChangesAsync();

            IReadOnlyCollection<ProposalItem> result = await service.GetItemsByProposalId(proposal.Id);

            result.Should().HaveCount(2);
        }
    }
}
