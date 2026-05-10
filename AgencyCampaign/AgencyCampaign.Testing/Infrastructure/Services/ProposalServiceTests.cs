using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProposalServiceTests
    {
        private TestDbContext db = null!;
        private Mock<IEmailService> emailService = null!;
        private Mock<IFinancialAutoGeneration> financial = null!;
        private Mock<IAutomationDispatcher> automation = null!;
        private Mock<INotificationService> notifications = null!;
        private ProposalService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            emailService = new Mock<IEmailService>();
            financial = new Mock<IFinancialAutoGeneration>();
            automation = new Mock<IAutomationDispatcher>();
            notifications = new Mock<INotificationService>();
            service = new ProposalService(db, LocalizerMock.Create<AgencyCampaignResource>(), CurrentUserMock.Create(),
                emailService.Object, financial.Object, automation.Object, notifications.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Opportunity> SeedOpportunityAsync(long? responsibleUserId = 7, string? responsibleUserName = "Owner")
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            db.Add(new Brand("Acme"));
            await db.SaveChangesAsync();

            Opportunity opportunity = new(brandId: 1, commercialPipelineStageId: 1, name: "Big deal", estimatedValue: 1000m,
                contactName: "Cliente", contactEmail: "cli@x", responsibleUserId: responsibleUserId, responsibleUserName: responsibleUserName);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            return opportunity;
        }

        [Test]
        public async Task CreateProposal_should_throw_when_opportunity_not_found()
        {
            CreateProposalRequest request = new() { OpportunityId = 99, ResponsibleUserId = 1 };
            Func<Task> act = () => service.CreateProposal(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateProposal_should_throw_when_no_responsible_user_resolvable()
        {
            Opportunity opportunity = await SeedOpportunityAsync(responsibleUserId: null, responsibleUserName: null);

            CreateProposalRequest request = new() { OpportunityId = opportunity.Id };
            Func<Task> act = () => service.CreateProposal(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateProposal_should_use_opportunity_responsible_when_request_lacks_one()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            proposal.InternalOwnerId.Should().Be(7);
            proposal.InternalOwnerName.Should().Be("Owner");
        }

        [Test]
        public async Task UpdateProposal_should_throw_when_id_mismatch()
        {
            UpdateProposalRequest request = new() { Id = 5, OpportunityId = 1 };
            Func<Task> act = () => service.UpdateProposal(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task MarkAsSent_should_create_proposal_version_and_notify_email()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            await service.MarkAsSent(proposal.Id);

            (await db.Set<ProposalVersion>().CountAsync()).Should().Be(1);
            emailService.Verify(item => item.SendForEvent(EmailEventType.ProposalSent, It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ApproveProposal_should_require_sent_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            Func<Task> act = () => service.ApproveProposal(proposal.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ApproveProposal_should_persist_status_and_notify()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);

            Proposal result = await service.ApproveProposal(proposal.Id);

            result.Status.Should().Be(ProposalStatus.Approved);
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RejectProposal_should_require_sent_or_viewed_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            Func<Task> act = () => service.RejectProposal(proposal.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ConvertToCampaign_should_throw_when_campaign_not_found()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            Func<Task> act = () => service.ConvertToCampaign(proposal.Id, campaignId: 999);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ConvertToCampaign_should_dispatch_financial_autogen_and_notification()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            db.Add(new Campaign(1, "Camp", 0m, DateTimeOffset.UtcNow).WithId(99));
            await db.SaveChangesAsync();

            Proposal result = await service.ConvertToCampaign(proposal.Id, campaignId: 99);

            result.Status.Should().Be(ProposalStatus.Converted);
            result.CampaignId.Should().Be(99);
            financial.Verify(item => item.GenerateForConvertedProposal(It.IsAny<Proposal>(), 99, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CancelProposal_should_persist_cancelled_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            Proposal result = await service.CancelProposal(proposal.Id);

            result.Status.Should().Be(ProposalStatus.Cancelled);
        }

        [Test]
        public async Task GetStatusHistory_should_throw_when_proposal_not_found()
        {
            Func<Task> act = () => service.GetStatusHistory(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
