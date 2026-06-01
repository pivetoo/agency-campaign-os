using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
            service = new ProposalShareLinkService(db, CurrentUserMock.Create(), TenantContextMock.Create());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Proposal> SeedProposalAsync(bool sent = true)
        {
            Proposal proposal = new(opportunityId: 1, name: "P", internalOwnerId: 1);
            if (sent)
            {
                proposal.MarkAsSent();
            }
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
        public async Task CreateShareLink_should_throw_when_proposal_is_in_draft()
        {
            Proposal proposal = await SeedProposalAsync(sent: false);

            Func<Task> act = () => service.CreateShareLink(proposal.Id, new CreateProposalShareLinkRequest());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("proposal.share.draftNotAllowed");
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
        public async Task CreateShareLink_should_default_expiry_when_not_provided()
        {
            Proposal proposal = await SeedProposalAsync();

            ProposalShareLinkModel link = await service.CreateShareLink(proposal.Id, new CreateProposalShareLinkRequest());

            link.ExpiresAt.Should().NotBeNull();
            link.ExpiresAt!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
        }

        [Test]
        public async Task RevokeShareLink_should_throw_when_not_found()
        {
            Func<Task> act = () => service.RevokeShareLink(1, 99);
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

            ProposalShareLinkModel result = await service.RevokeShareLink(proposal.Id, link.Id);

            result.RevokedAt.Should().NotBeNull();
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task RevokeShareLink_should_throw_when_link_belongs_to_another_proposal()
        {
            Proposal owner = await SeedProposalAsync();
            Proposal other = await SeedProposalAsync();
            ProposalShareLink link = new(owner.Id, "tok", null, null, null);
            db.Add(link);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.RevokeShareLink(other.Id, link.Id);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetByProposalId_should_throw_when_proposal_not_found()
        {
            Func<Task> act = () => service.GetByProposalId(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetEngagement_should_aggregate_views_across_links_with_timeline()
        {
            Proposal proposal = await SeedProposalAsync();

            ProposalShareLink link = new(proposal.Id, "tok", null, null, null);
            link.RegisterView("1.2.3.0", "Mozilla/5.0 (iPhone; CPU iPhone OS) Mobile Safari");
            link.RegisterView("4.5.6.0", "Mozilla/5.0 (Windows NT 10.0) Chrome");
            db.Add(link);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            ProposalEngagementModel result = await service.GetEngagement(proposal.Id);

            result.TotalViews.Should().Be(2);
            result.ActiveLinks.Should().Be(1);
            result.Events.Should().HaveCount(2);
            result.FirstViewedAt.Should().NotBeNull();
            result.LastViewedAt.Should().NotBeNull();
            result.Events.Should().Contain(item => item.Device == "mobile");
            result.Events.Should().Contain(item => item.Device == "desktop");
        }

        [Test]
        public async Task GetEngagement_should_return_empty_when_no_views()
        {
            Proposal proposal = await SeedProposalAsync();

            ProposalEngagementModel result = await service.GetEngagement(proposal.Id);

            result.TotalViews.Should().Be(0);
            result.Events.Should().BeEmpty();
            result.FirstViewedAt.Should().BeNull();
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
