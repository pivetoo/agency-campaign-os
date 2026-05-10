using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

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
            service = new ProposalItemService(db, LocalizerMock.Create<AgencyCampaignResource>());
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
