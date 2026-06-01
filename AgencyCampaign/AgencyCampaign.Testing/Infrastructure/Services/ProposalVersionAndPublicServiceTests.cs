using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProposalVersionServiceTests
    {
        private TestDbContext db = null!;
        private ProposalVersionService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new ProposalVersionService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task GetByProposalId_should_throw_when_proposal_not_found()
        {
            Func<Task> act = () => service.GetByProposalId(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetByProposalId_should_order_by_version_descending()
        {
            Proposal proposal = new(1, "P", 1);
            db.Add(proposal);
            await db.SaveChangesAsync();

            db.Add(new ProposalVersion(proposal.Id, 1, "v1", null, 100m, null, "{}", null, null));
            db.Add(new ProposalVersion(proposal.Id, 2, "v2", null, 200m, null, "{}", null, null));
            db.Add(new ProposalVersion(proposal.Id, 3, "v3", null, 300m, null, "{}", null, null));
            await db.SaveChangesAsync();

            IReadOnlyCollection<ProposalVersionModel> result = await service.GetByProposalId(proposal.Id);

            result.Select(item => item.VersionNumber).Should().Equal(3, 2, 1);
        }

        [Test]
        public async Task GetById_should_return_null_when_not_found()
        {
            (await service.GetById(99)).Should().BeNull();
        }
    }

    [TestFixture]
    public sealed class ProposalPublicServiceTests
    {
        private TestDbContext db = null!;
        private Mock<INotificationService> notifications = null!;
        private ProposalPublicService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new Mock<INotificationService>();
            service = new ProposalPublicService(db, notifications.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task GetByToken_should_return_null_for_blank_or_missing()
        {
            (await service.GetByToken(" ", null, null)).Should().BeNull();
            (await service.GetByToken("missing", null, null)).Should().BeNull();
        }

        [Test]
        public async Task GetByToken_should_return_null_when_share_link_inactive()
        {
            ProposalShareLink link = new(1, "tok", DateTimeOffset.UtcNow.AddMinutes(-1), null, null);
            db.Add(link);
            await db.SaveChangesAsync();

            (await service.GetByToken("tok", null, null)).Should().BeNull();
        }

        [Test]
        public async Task GetByToken_should_return_null_when_no_version_exists()
        {
            Proposal proposal = new(1, "P", 1);
            db.Add(proposal);
            await db.SaveChangesAsync();

            ProposalShareLink link = new(proposal.Id, "tok", null, null, null);
            db.Add(link);
            await db.SaveChangesAsync();

            (await service.GetByToken("tok", null, null)).Should().BeNull();
        }

        [Test]
        public async Task GetByToken_should_register_view_and_notify_on_first_view()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();
            CommercialPipelineStage stage = new("Q", 1, "#fff");
            db.Add(stage);
            await db.SaveChangesAsync();
            Opportunity opportunity = new(brand.Id, stage.Id, "deal", 0m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            Proposal proposal = new(opportunity.Id, "P", 1);
            db.Add(proposal);
            await db.SaveChangesAsync();
            db.Add(new ProposalVersion(proposal.Id, 1, "v1", null, 100m, null, "{}", null, null));
            db.Add(new ProposalShareLink(proposal.Id, "tok", null, null, null));
            await db.SaveChangesAsync();

            ProposalPublicViewModel? viewModel = await service.GetByToken("tok", "1.2.3.4", "agent");

            viewModel.Should().NotBeNull();
            viewModel!.VersionNumber.Should().Be(1);
            viewModel.BrandName.Should().Be("Acme");
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetByToken_should_promote_sent_proposal_to_viewed()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();
            CommercialPipelineStage stage = new("Q", 1, "#fff");
            db.Add(stage);
            await db.SaveChangesAsync();
            Opportunity opportunity = new(brand.Id, stage.Id, "deal", 0m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            Proposal proposal = new(opportunity.Id, "P", 1);
            proposal.MarkAsSent();
            db.Add(proposal);
            await db.SaveChangesAsync();
            db.Add(new ProposalVersion(proposal.Id, 1, "v1", null, 100m, null, "{}", null, null));
            db.Add(new ProposalShareLink(proposal.Id, "tok", null, null, null));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await service.GetByToken("tok", null, null);

            db.ChangeTracker.Clear();
            Proposal reloaded = await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == proposal.Id);
            reloaded.Status.Should().Be(ProposalStatus.Viewed);
        }

        [Test]
        public async Task GetByToken_should_strip_internal_notes_from_public_snapshot()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();
            CommercialPipelineStage stage = new("Q", 1, "#fff");
            db.Add(stage);
            await db.SaveChangesAsync();
            Opportunity opportunity = new(brand.Id, stage.Id, "deal", 0m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            Proposal proposal = new(opportunity.Id, "P", 1);
            db.Add(proposal);
            await db.SaveChangesAsync();
            string snapshot = "{\"name\":\"Proposta\",\"notes\":\"segredo interno da agencia\",\"items\":[]}";
            db.Add(new ProposalVersion(proposal.Id, 1, "v1", null, 100m, null, snapshot, null, null));
            db.Add(new ProposalShareLink(proposal.Id, "tok", null, null, null));
            await db.SaveChangesAsync();

            ProposalPublicViewModel? viewModel = await service.GetByToken("tok", null, null);

            viewModel.Should().NotBeNull();
            viewModel!.SnapshotJson.Should().NotContain("segredo interno");
            viewModel.SnapshotJson.Should().NotContain("notes");
        }

        [Test]
        public async Task GetByToken_should_return_null_when_proposal_is_rejected()
        {
            Proposal proposal = new(1, "P", 1);
            proposal.MarkAsSent();
            proposal.Reject();
            db.Add(proposal);
            await db.SaveChangesAsync();
            db.Add(new ProposalVersion(proposal.Id, 1, "v1", null, 100m, null, "{}", null, null));
            db.Add(new ProposalShareLink(proposal.Id, "tok", null, null, null));
            await db.SaveChangesAsync();

            (await service.GetByToken("tok", null, null)).Should().BeNull();
        }

        [Test]
        public async Task GetByToken_should_return_null_when_proposal_validity_passed()
        {
            Proposal proposal = new(1, "P", 1, validityUntil: DateTimeOffset.UtcNow.AddDays(-1));
            db.Add(proposal);
            await db.SaveChangesAsync();
            db.Add(new ProposalVersion(proposal.Id, 1, "v1", null, 100m, null, "{}", null, null));
            db.Add(new ProposalShareLink(proposal.Id, "tok", null, null, null));
            await db.SaveChangesAsync();

            (await service.GetByToken("tok", null, null)).Should().BeNull();
        }

        [Test]
        public async Task GetByToken_should_use_frozen_discount_from_version_not_live_proposal()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();
            CommercialPipelineStage stage = new("Q", 1, "#fff");
            db.Add(stage);
            await db.SaveChangesAsync();
            Opportunity opportunity = new(brand.Id, stage.Id, "deal", 0m);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            Proposal proposal = new(opportunity.Id, "P", 1, discountAmount: 500m);
            proposal.UpdateTotalValue(1000m);
            db.Add(proposal);
            await db.SaveChangesAsync();
            db.Add(new ProposalVersion(proposal.Id, 1, "v1", null, 1000m, null, "{}", null, null, discountAmount: 200m, netTotalValue: 800m));
            db.Add(new ProposalShareLink(proposal.Id, "tok", null, null, null));
            await db.SaveChangesAsync();

            ProposalPublicViewModel? viewModel = await service.GetByToken("tok", null, null);

            viewModel.Should().NotBeNull();
            viewModel!.NetTotalValue.Should().Be(800m);
            viewModel.DiscountValue.Should().Be(200m);
        }
    }
}
