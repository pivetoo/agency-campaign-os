using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProposalServiceTests
    {
        private TestDbContext db = null!;
        private Mock<IFinancialAutoGeneration> financial = null!;
        private Mock<IAutomationDispatcher> automation = null!;
        private Mock<INotificationService> notifications = null!;
        private IPolicyEvaluator policyEvaluator = null!;
        private IOpportunityApprovalRequestService approvalRequestService = null!;
        private ProposalService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            financial = new Mock<IFinancialAutoGeneration>();
            automation = new Mock<IAutomationDispatcher>();
            notifications = new Mock<INotificationService>();
            policyEvaluator = new PolicyEvaluatorService(db);
            approvalRequestService = new OpportunityApprovalRequestService(db, notifications.Object, policyEvaluator, CurrentUserMock.Create());
            service = new ProposalService(db, CurrentUserMock.Create(), TenantContextMock.Create(), financial.Object, automation.Object, notifications.Object, IntegrationPlatformClientFactory.CreateInert(), new IntegrationCapabilityService(db), policyEvaluator, approvalRequestService);
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
        public async Task MarkAsSent_should_create_proposal_version_and_dispatch_automation()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            await service.MarkAsSent(proposal.Id);

            (await db.Set<ProposalVersion>().CountAsync()).Should().Be(1);
            automation.Verify(item => item.DispatchAsync(AutomationTriggers.ProposalSent,
                It.IsAny<IDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
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
        public async Task ConvertToNewCampaign_should_throw_when_not_approved()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            Func<Task> act = () => service.ConvertToNewCampaign(proposal.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ConvertToNewCampaign_should_create_linked_campaign_and_convert()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            Proposal result = await service.ConvertToNewCampaign(proposal.Id);

            result.Status.Should().Be(ProposalStatus.Converted);
            result.CampaignId.Should().NotBeNull();

            db.ChangeTracker.Clear();
            Campaign campaign = await db.Set<Campaign>().AsNoTracking().SingleAsync();
            campaign.Id.Should().Be(result.CampaignId!.Value);
            campaign.BrandId.Should().Be(opportunity.BrandId);
            campaign.OpportunityId.Should().Be(opportunity.Id);
            campaign.SourceProposalId.Should().Be(proposal.Id);
            financial.Verify(item => item.GenerateForConvertedProposal(It.IsAny<Proposal>(), campaign.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ConvertToNewCampaign_should_carry_responsible_user_id_from_proposal()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            await service.ConvertToNewCampaign(proposal.Id);

            db.ChangeTracker.Clear();
            Campaign campaign = await db.Set<Campaign>().AsNoTracking().SingleAsync();
            campaign.ResponsibleUserId.Should().Be(proposal.InternalOwnerId);
        }

        [Test]
        public async Task ConvertToNewCampaign_should_apply_name_and_start_date_overrides()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            DateTimeOffset start = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
            await service.ConvertToNewCampaign(proposal.Id, "Campanha de Inverno", start);

            db.ChangeTracker.Clear();
            Campaign campaign = await db.Set<Campaign>().AsNoTracking().SingleAsync();
            campaign.Name.Should().Be("Campanha de Inverno");
            campaign.StartsAt.Should().Be(start);
        }

        [Test]
        public async Task ConvertToNewCampaign_should_set_campaign_budget_to_net_total()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest
            {
                OpportunityId = opportunity.Id,
                DiscountAmount = 200m
            });
            await SetProposalTotalAsync(proposal.Id, 1000m);
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            await service.ConvertToNewCampaign(proposal.Id);

            db.ChangeTracker.Clear();
            Campaign campaign = await db.Set<Campaign>().AsNoTracking().SingleAsync();
            campaign.Budget.Should().Be(800m);
        }

        [Test]
        public async Task ConvertToNewCampaign_should_seed_campaign_creators_from_proposal_items()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).AsInitial().Build());
            Creator creatorA = new("Ana");
            Creator creatorB = new("Bru");
            db.Add(creatorA);
            db.Add(creatorB);
            await db.SaveChangesAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            db.Add(new ProposalItem(proposal.Id, "Post", 1, 500m, creatorId: creatorA.Id));
            db.Add(new ProposalItem(proposal.Id, "Story", 1, 300m, creatorId: creatorA.Id));
            db.Add(new ProposalItem(proposal.Id, "Reel", 2, 100m, creatorId: creatorB.Id));
            db.Add(new ProposalItem(proposal.Id, "Sem creator", 1, 50m));
            await db.SaveChangesAsync();
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            Proposal converted = await service.ConvertToNewCampaign(proposal.Id);

            long campaignId = converted.CampaignId!.Value;
            db.ChangeTracker.Clear();
            List<CampaignCreator> seeded = await db.Set<CampaignCreator>().AsNoTracking().Where(item => item.CampaignId == campaignId).ToListAsync();
            seeded.Should().HaveCount(2);
            seeded.Single(item => item.CreatorId == creatorA.Id).AgreedAmount.Should().Be(800m);
            seeded.Single(item => item.CreatorId == creatorB.Id).AgreedAmount.Should().Be(200m);
        }

        [Test]
        public async Task ConvertToNewCampaign_should_propagate_financial_failure_instead_of_swallowing()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);

            financial
                .Setup(item => item.GenerateForConvertedProposal(It.IsAny<Proposal>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("financial boom"));

            Func<Task> act = () => service.ConvertToNewCampaign(proposal.Id);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financial boom");
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

        [Test]
        public async Task GetProposalById_should_return_null_when_not_found()
        {
            (await service.GetProposalById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetProposalById_should_return_proposal_when_found()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            Proposal? result = await service.GetProposalById(proposal.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetProposals_should_filter_by_opportunity()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });

            Archon.Core.Pagination.PagedResult<Proposal> result = await service.GetProposals(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, new ProposalListFilters { OpportunityId = opportunity.Id });

            result.Items.Should().HaveCount(1);
        }

        [Test]
        public async Task GetProposals_should_filter_by_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal p1 = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            Proposal p2 = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(p2.Id);

            Archon.Core.Pagination.PagedResult<Proposal> result = await service.GetProposals(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 }, new ProposalListFilters { Status = (int)ProposalStatus.Sent });

            result.Items.Should().ContainSingle(item => item.Id == p2.Id);
        }

        [Test]
        public async Task UpdateProposal_should_throw_when_not_found()
        {
            UpdateProposalRequest request = new() { Id = 99, OpportunityId = 1 };

            Func<Task> act = () => service.UpdateProposal(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateProposal_should_persist_changes()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            db.ChangeTracker.Clear();

            UpdateProposalRequest request = new()
            {
                Id = proposal.Id,
                OpportunityId = opportunity.Id,
                Description = "Updated description",
                ValidityUntil = DateTimeOffset.UtcNow.AddDays(30)
            };

            Proposal result = await service.UpdateProposal(proposal.Id, request);

            result.Description.Should().Be("Updated description");
        }

        [Test]
        public async Task MarkAsViewed_should_set_viewed_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);

            Proposal result = await service.MarkAsViewed(proposal.Id);

            result.Status.Should().Be(ProposalStatus.Viewed);
        }

        [Test]
        public async Task RejectProposal_should_persist_when_sent()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);

            Proposal result = await service.RejectProposal(proposal.Id);

            result.Status.Should().Be(ProposalStatus.Rejected);
        }

        [Test]
        public async Task GetStatusHistory_should_return_history_when_proposal_exists()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);

            IReadOnlyCollection<AgencyCampaign.Application.Models.Commercial.ProposalStatusHistoryModel> result = await service.GetStatusHistory(proposal.Id);

            result.Should().NotBeEmpty();
        }

        [Test]
        public async Task CreateProposal_should_persist_discount_and_payment_term()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest
            {
                OpportunityId = opportunity.Id,
                DiscountAmount = 150m,
                PaymentTermDays = 45
            });

            proposal.DiscountAmount.Should().Be(150m);
            proposal.PaymentTermDays.Should().Be(45);
        }

        private async Task SetProposalTotalAsync(long proposalId, decimal totalValue)
        {
            Proposal tracked = await db.Set<Proposal>().AsTracking().SingleAsync(item => item.Id == proposalId);
            tracked.UpdateTotalValue(totalValue);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        [Test]
        public async Task MarkAsSent_should_block_and_create_approval_when_proposal_deviates_from_policy()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new CommercialPolicy(maxDiscountPercent: 10m, defaultPaymentTermDays: null, maxPaymentTermDays: null));
            await db.SaveChangesAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest
            {
                OpportunityId = opportunity.Id,
                DiscountAmount = 300m
            });
            await SetProposalTotalAsync(proposal.Id, 1000m);

            Func<Task> act = () => service.MarkAsSent(proposal.Id);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("proposal.send.approvalRequired");

            db.ChangeTracker.Clear();
            (await db.Set<OpportunityApprovalRequest>().CountAsync(item => item.ProposalId == proposal.Id)).Should().Be(1);
        }

        [Test]
        public async Task MarkAsSent_should_not_duplicate_approval_when_one_is_already_pending()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new CommercialPolicy(maxDiscountPercent: 10m, defaultPaymentTermDays: null, maxPaymentTermDays: null));
            await db.SaveChangesAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest
            {
                OpportunityId = opportunity.Id,
                DiscountAmount = 300m
            });
            await SetProposalTotalAsync(proposal.Id, 1000m);

            await service.Invoking(s => s.MarkAsSent(proposal.Id)).Should().ThrowAsync<InvalidOperationException>();
            db.ChangeTracker.Clear();
            await service.Invoking(s => s.MarkAsSent(proposal.Id)).Should().ThrowAsync<InvalidOperationException>();

            db.ChangeTracker.Clear();
            (await db.Set<OpportunityApprovalRequest>().CountAsync(item => item.ProposalId == proposal.Id)).Should().Be(1);
        }

        [Test]
        public async Task MarkAsSent_should_succeed_after_internal_approval_is_approved()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new CommercialPolicy(maxDiscountPercent: 10m, defaultPaymentTermDays: null, maxPaymentTermDays: null));
            await db.SaveChangesAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest
            {
                OpportunityId = opportunity.Id,
                DiscountAmount = 300m
            });
            await SetProposalTotalAsync(proposal.Id, 1000m);

            await service.Invoking(s => s.MarkAsSent(proposal.Id)).Should().ThrowAsync<InvalidOperationException>();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequest approval = await db.Set<OpportunityApprovalRequest>()
                .AsTracking()
                .SingleAsync(item => item.ProposalId == proposal.Id);
            await approvalRequestService.Approve(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });
            db.ChangeTracker.Clear();

            await service.MarkAsSent(proposal.Id);

            db.ChangeTracker.Clear();
            (await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == proposal.Id)).Status.Should().Be(ProposalStatus.Sent);
        }

        [Test]
        public async Task MarkAsSent_should_succeed_after_approval_is_merged_without_creating_new_approval()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new CommercialPolicy(maxDiscountPercent: 10m, defaultPaymentTermDays: null, maxPaymentTermDays: null));
            await db.SaveChangesAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest
            {
                OpportunityId = opportunity.Id,
                DiscountAmount = 300m
            });
            await SetProposalTotalAsync(proposal.Id, 1000m);

            await service.Invoking(s => s.MarkAsSent(proposal.Id)).Should().ThrowAsync<InvalidOperationException>();
            db.ChangeTracker.Clear();

            OpportunityApprovalRequest approval = await db.Set<OpportunityApprovalRequest>()
                .AsTracking()
                .SingleAsync(item => item.ProposalId == proposal.Id);
            await approvalRequestService.Approve(approval.Id, new DecideOpportunityApprovalRequest { ApprovedByUserName = "Boss" });
            db.ChangeTracker.Clear();
            await approvalRequestService.MarkMerged(approval.Id);
            db.ChangeTracker.Clear();

            await service.MarkAsSent(proposal.Id);

            db.ChangeTracker.Clear();
            (await db.Set<OpportunityApprovalRequest>().CountAsync(item => item.ProposalId == proposal.Id)).Should().Be(1);
            (await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == proposal.Id)).Status.Should().Be(ProposalStatus.Sent);
        }

        [Test]
        public async Task MarkAsSent_should_succeed_when_proposal_within_policy()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new CommercialPolicy(maxDiscountPercent: 30m, defaultPaymentTermDays: null, maxPaymentTermDays: null));
            await db.SaveChangesAsync();

            Proposal proposal = await service.CreateProposal(new CreateProposalRequest
            {
                OpportunityId = opportunity.Id,
                DiscountAmount = 100m
            });
            await SetProposalTotalAsync(proposal.Id, 1000m);

            await service.MarkAsSent(proposal.Id);

            db.ChangeTracker.Clear();
            (await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == proposal.Id)).Status.Should().Be(ProposalStatus.Sent);
            (await db.Set<OpportunityApprovalRequest>().CountAsync(item => item.ProposalId == proposal.Id)).Should().Be(0);
        }

        [Test]
        public async Task ExpireOverdue_should_expire_only_sent_proposals_past_validity()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            Proposal overdue = new(opportunity.Id, "Overdue", 1, validityUntil: DateTimeOffset.UtcNow.AddDays(-1));
            overdue.MarkAsSent();
            Proposal future = new(opportunity.Id, "Future", 1, validityUntil: DateTimeOffset.UtcNow.AddDays(5));
            future.MarkAsSent();
            db.Add(overdue);
            db.Add(future);
            await db.SaveChangesAsync();

            int count = await service.ExpireOverdue();

            count.Should().Be(1);
            db.ChangeTracker.Clear();
            (await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == overdue.Id)).Status.Should().Be(ProposalStatus.Expired);
            (await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == future.Id)).Status.Should().Be(ProposalStatus.Sent);
        }

        [Test]
        public async Task RemindExpiringSoon_should_remind_once_for_proposals_within_window()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            Proposal expiringSoon = new(opportunity.Id, "Expiring", 1, validityUntil: DateTimeOffset.UtcNow.AddDays(2));
            expiringSoon.MarkAsSent();
            Proposal farOut = new(opportunity.Id, "Far", 1, validityUntil: DateTimeOffset.UtcNow.AddDays(10));
            farOut.MarkAsSent();
            Proposal alreadyExpired = new(opportunity.Id, "Past", 1, validityUntil: DateTimeOffset.UtcNow.AddDays(-1));
            alreadyExpired.MarkAsSent();
            db.Add(expiringSoon);
            db.Add(farOut);
            db.Add(alreadyExpired);
            await db.SaveChangesAsync();

            int reminded = await service.RemindExpiringSoon(3);

            reminded.Should().Be(1);
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            db.ChangeTracker.Clear();
            (await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == expiringSoon.Id)).ExpiryReminderSentAt.Should().NotBeNull();

            int second = await service.RemindExpiringSoon(3);
            second.Should().Be(0);
        }

        [Test]
        public async Task MarkAsSent_should_reject_resend_of_already_approved_proposal_without_new_version()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            await service.MarkAsSent(proposal.Id);
            await service.ApproveProposal(proposal.Id);
            db.ChangeTracker.Clear();

            await service.Invoking(s => s.MarkAsSent(proposal.Id)).Should().ThrowAsync<InvalidOperationException>();

            db.ChangeTracker.Clear();
            (await db.Set<ProposalVersion>().CountAsync(item => item.ProposalId == proposal.Id)).Should().Be(1);
            (await db.Set<Proposal>().AsNoTracking().SingleAsync(item => item.Id == proposal.Id)).Status.Should().Be(ProposalStatus.Approved);
        }

        [Test]
        public async Task SendByEmail_should_create_share_link_with_tenant_prefix_and_default_expiration()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            Proposal proposal = await service.CreateProposal(new CreateProposalRequest { OpportunityId = opportunity.Id });
            db.Add(new IntegrationCapability(IntegrationIntents.ProposalSendEmail, connectorId: 1));
            await db.SaveChangesAsync();

            // O cliente inerte lanca no enqueue, mas versao/link/status ja sao persistidos antes (comportamento real do envio)
            SendProposalEmailRequest request = new() { RecipientEmail = "cli@x.com", Subject = "Proposta", Body = "Segue proposta" };
            await service.Invoking(s => s.SendByEmail(proposal.Id, request))
                .Should().ThrowAsync<InvalidOperationException>().WithMessage("integrationPlatform.notConfigured");

            db.ChangeTracker.Clear();
            ProposalShareLink link = await db.Set<ProposalShareLink>().AsNoTracking().SingleAsync(item => item.ProposalId == proposal.Id);
            PublicLinkToken.ExtractTenantId(link.Token).Should().Be("tenant-1");
            link.ExpiresAt.Should().NotBeNull();
        }
    }
}
