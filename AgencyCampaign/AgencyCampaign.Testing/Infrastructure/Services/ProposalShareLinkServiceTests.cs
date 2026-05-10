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
    public sealed class ProposalShareLinkServiceTests
    {
        private TestDbContext db = null!;
        private ProposalShareLinkService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new ProposalShareLinkService(db, CurrentUserMock.Create(), LocalizerMock.Create<AgencyCampaignResource>());
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
        public async Task CreateShareLink_should_throw_when_proposal_not_found()
        {
            Func<Task> act = () => service.CreateShareLink(99, new CreateProposalShareLinkRequest());
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateShareLink_should_persist_with_unique_token()
        {
            Proposal proposal = await SeedProposalAsync();

            ProposalShareLinkModel a = await service.CreateShareLink(proposal.Id, new CreateProposalShareLinkRequest());
            ProposalShareLinkModel b = await service.CreateShareLink(proposal.Id, new CreateProposalShareLinkRequest());

            a.Token.Should().NotBe(b.Token);
            a.Token.Should().NotContainAny("+", "/", "=");
            a.IsActive.Should().BeTrue();
        }

        [Test]
        public async Task RevokeShareLink_should_throw_when_not_found()
        {
            Func<Task> act = () => service.RevokeShareLink(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task RevokeShareLink_should_set_revokedAt_and_inactivate()
        {
            Proposal proposal = await SeedProposalAsync();
            ProposalShareLink link = new(proposal.Id, "tok", null, null, null);
            db.Add(link);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            ProposalShareLinkModel result = await service.RevokeShareLink(link.Id);

            result.RevokedAt.Should().NotBeNull();
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetByProposalId_should_throw_when_proposal_not_found()
        {
            Func<Task> act = () => service.GetByProposalId(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetByProposalId_should_compute_is_active_from_state()
        {
            Proposal proposal = await SeedProposalAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            ProposalShareLink active = new(proposal.Id, "active", now.AddDays(7), null, null);
            ProposalShareLink expired = new(proposal.Id, "expired", now.AddMinutes(-1), null, null);
            ProposalShareLink revoked = new(proposal.Id, "revoked", null, null, null);
            revoked.Revoke();

            db.Add(active);
            db.Add(expired);
            db.Add(revoked);
            await db.SaveChangesAsync();

            IReadOnlyCollection<ProposalShareLinkModel> result = await service.GetByProposalId(proposal.Id);

            result.Single(item => item.Token == "active").IsActive.Should().BeTrue();
            result.Single(item => item.Token == "expired").IsActive.Should().BeFalse();
            result.Single(item => item.Token == "revoked").IsActive.Should().BeFalse();
        }
    }
}
