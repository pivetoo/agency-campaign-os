using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Application.Services;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalService : CrudService<Proposal>, IProposalService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly ICurrentUser currentUser;
        private readonly IEmailService emailService;
        private readonly IFinancialAutoGeneration financialAutoGeneration;
        private readonly IAutomationDispatcher automationDispatcher;
        private readonly INotificationService notificationService;

        public ProposalService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, ICurrentUser currentUser, IEmailService emailService, IFinancialAutoGeneration financialAutoGeneration, IAutomationDispatcher automationDispatcher, INotificationService notificationService) : base(dbContext)
        {
            this.localizer = localizer;
            this.currentUser = currentUser;
            this.emailService = emailService;
            this.financialAutoGeneration = financialAutoGeneration;
            this.automationDispatcher = automationDispatcher;
            this.notificationService = notificationService;
        }

        public async Task<PagedResult<Proposal>> GetProposals(PagedRequest request, ProposalListFilters filters, CancellationToken cancellationToken = default)
        {
            IQueryable<Proposal> query = QueryWithDetails();
            query = ApplyProposalFilters(query, filters);

            return await query
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        private static IQueryable<Proposal> ApplyProposalFilters(IQueryable<Proposal> query, ProposalListFilters filters)
        {
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                string term = filters.Search.Trim().ToLower();
                query = query.Where(item =>
                    item.Name.ToLower().Contains(term)
                    || (item.Opportunity != null && item.Opportunity.Name.ToLower().Contains(term))
                    || (item.Opportunity != null && item.Opportunity.Brand != null && item.Opportunity.Brand.Name.ToLower().Contains(term)));
            }

            if (filters.Status.HasValue)
            {
                ProposalStatus statusValue = (ProposalStatus)filters.Status.Value;
                query = query.Where(item => item.Status == statusValue);
            }

            if (filters.OpportunityId.HasValue)
            {
                query = query.Where(item => item.OpportunityId == filters.OpportunityId.Value);
            }

            if (filters.InternalOwnerId.HasValue)
            {
                query = query.Where(item => item.InternalOwnerId == filters.InternalOwnerId.Value);
            }

            if (filters.ValidityFrom.HasValue)
            {
                query = query.Where(item => item.ValidityUntil.HasValue && item.ValidityUntil.Value >= filters.ValidityFrom.Value);
            }

            if (filters.ValidityTo.HasValue)
            {
                query = query.Where(item => item.ValidityUntil.HasValue && item.ValidityUntil.Value <= filters.ValidityTo.Value);
            }

            return query;
        }

        public async Task<Proposal?> GetProposalById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Proposal> CreateProposal(CreateProposalRequest request, CancellationToken cancellationToken = default)
        {
            Opportunity opportunity = await GetOpportunity(request.OpportunityId, cancellationToken);

            long responsibleUserId = request.ResponsibleUserId ?? opportunity.ResponsibleUserId ?? throw new InvalidOperationException(localizer["proposal.responsibleUser.required"]);
            string commercialResponsibleName = opportunity.ResponsibleUserName ?? string.Empty;

            Proposal proposal = new(
                request.OpportunityId,
                opportunity.Name,
                responsibleUserId,
                request.Description,
                request.ValidityUntil,
                request.Notes,
                currentUser.UserId,
                currentUser.UserName);

            if (!string.IsNullOrWhiteSpace(commercialResponsibleName))
            {
                proposal.SetInternalOwner(responsibleUserId, commercialResponsibleName);
            }

            bool success = await Insert(cancellationToken, proposal);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetProposalById(proposal.Id, cancellationToken) ?? proposal;
        }

        public async Task<Proposal> UpdateProposal(long id, UpdateProposalRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            Opportunity opportunity = await GetOpportunity(request.OpportunityId, cancellationToken);

            proposal.Update(
                opportunity.Name,
                request.ValidityUntil,
                request.Description,
                request.Notes,
                request.OpportunityId);

            Proposal? result = await Update(proposal, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetProposalById(result.Id, cancellationToken) ?? result;
        }

        public async Task<Proposal> MarkAsSent(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .Include(item => item.Items)
                    .ThenInclude(item => item.Creator)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            int nextVersion = await DbContext.Set<ProposalVersion>()
                .Where(item => item.ProposalId == id)
                .CountAsync(cancellationToken) + 1;

            string snapshotJson = SerializeSnapshot(proposal);

            ProposalVersion version = new(
                proposal.Id,
                nextVersion,
                proposal.Name,
                proposal.Description,
                proposal.TotalValue,
                proposal.ValidityUntil,
                snapshotJson,
                currentUser.UserId,
                currentUser.UserName);

            DbContext.Set<ProposalVersion>().Add(version);

            proposal.MarkAsSent(currentUser.UserId, currentUser.UserName);

            Proposal saved = await SaveAndReturn(proposal, cancellationToken);
            await NotifyEmail(EmailEventType.ProposalSent, saved, cancellationToken);
            return saved;
        }

        private static string SerializeSnapshot(Proposal proposal)
        {
            var snapshot = new
            {
                proposalId = proposal.Id,
                name = proposal.Name,
                description = proposal.Description,
                totalValue = proposal.TotalValue,
                validityUntil = proposal.ValidityUntil,
                notes = proposal.Notes,
                items = proposal.Items.Select(item => new
                {
                    id = item.Id,
                    creatorId = item.CreatorId,
                    creatorName = item.Creator?.Name,
                    description = item.Description,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                    total = item.Total,
                    deliveryDeadline = item.DeliveryDeadline,
                    observations = item.Observations,
                    status = (int)item.Status
                }).ToArray()
            };

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public async Task<Proposal> MarkAsViewed(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.MarkAsViewed(currentUser.UserId, currentUser.UserName);

            return await SaveAndReturn(proposal, cancellationToken);
        }

        public async Task<Proposal> ApproveProposal(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.Approve(currentUser.UserId, currentUser.UserName);

            Proposal saved = await SaveAndReturn(proposal, cancellationToken);
            await NotifyEmail(EmailEventType.ProposalApproved, saved, cancellationToken);
            await TryNotify(KanvasNotifications.ProposalApproved(saved), cancellationToken);
            return saved;
        }

        public async Task<Proposal> RejectProposal(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.Reject(currentUser.UserId, currentUser.UserName);

            Proposal saved = await SaveAndReturn(proposal, cancellationToken);
            await NotifyEmail(EmailEventType.ProposalRejected, saved, cancellationToken);
            await TryNotify(KanvasNotifications.ProposalRejected(saved), cancellationToken);
            return saved;
        }

        public async Task<Proposal> ConvertToCampaign(long id, long campaignId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);

            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            proposal.ConvertToCampaign(campaignId, currentUser.UserId, currentUser.UserName);

            Proposal saved = await SaveAndReturn(proposal, cancellationToken);

            try
            {
                await financialAutoGeneration.GenerateForConvertedProposal(saved, campaignId, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[ProposalService] failed to generate financial entry for proposal {saved.Id}: {exception.Message}");
            }

            await NotifyEmail(EmailEventType.ProposalConverted, saved, cancellationToken);
            await TryNotify(KanvasNotifications.ProposalConverted(saved, campaignId), cancellationToken);
            return saved;
        }

        private async Task TryNotify(Archon.Core.Notifications.CreateNotificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await notificationService.Create(request, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[ProposalService] failed to create notification: {exception.Message}");
            }
        }

        private async Task NotifyEmail(EmailEventType eventType, Proposal proposal, CancellationToken cancellationToken)
        {
            Dictionary<string, object?> payload = new(StringComparer.OrdinalIgnoreCase)
            {
                ["proposalId"] = proposal.Id,
                ["proposalName"] = proposal.Name,
                ["totalValue"] = proposal.TotalValue,
                ["validityUntil"] = proposal.ValidityUntil?.ToString("dd/MM/yyyy"),
                ["opportunityName"] = proposal.Opportunity?.Name,
                ["brandName"] = proposal.Opportunity?.Brand?.Name,
                ["contactName"] = proposal.Opportunity?.ContactName,
                ["contactEmail"] = proposal.Opportunity?.ContactEmail,
                ["responsibleName"] = proposal.InternalOwnerName
            };

            string? recipient = proposal.Opportunity?.ContactEmail;
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                try
                {
                    await emailService.SendForEvent(eventType, new[] { recipient }, payload, cancellationToken);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"[ProposalService] failed to dispatch email for {eventType}: {exception.Message}");
                }
            }

            string trigger = eventType switch
            {
                EmailEventType.ProposalSent => AutomationTriggers.ProposalSent,
                EmailEventType.ProposalApproved => AutomationTriggers.ProposalApproved,
                EmailEventType.ProposalRejected => AutomationTriggers.ProposalRejected,
                EmailEventType.ProposalConverted => AutomationTriggers.ProposalConverted,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(trigger))
            {
                await automationDispatcher.DispatchAsync(trigger, payload, cancellationToken);
            }
        }

        public async Task<Proposal> CancelProposal(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.Cancel(currentUser.UserId, currentUser.UserName);

            return await SaveAndReturn(proposal, cancellationToken);
        }

        public async Task<IReadOnlyCollection<ProposalStatusHistoryModel>> GetStatusHistory(long proposalId, CancellationToken cancellationToken = default)
        {
            bool exists = await DbContext.Set<Proposal>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == proposalId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return await DbContext.Set<ProposalStatusHistory>()
                .AsNoTracking()
                .Where(item => item.ProposalId == proposalId)
                .OrderByDescending(item => item.ChangedAt)
                .Select(item => new ProposalStatusHistoryModel
                {
                    Id = item.Id,
                    ProposalId = item.ProposalId,
                    FromStatus = item.FromStatus.HasValue ? (int)item.FromStatus.Value : null,
                    ToStatus = (int)item.ToStatus,
                    ChangedAt = item.ChangedAt,
                    ChangedByUserId = item.ChangedByUserId,
                    ChangedByUserName = item.ChangedByUserName,
                    Reason = item.Reason
                })
                .ToArrayAsync(cancellationToken);
        }

        private async Task<Proposal> GetAndValidateProposal(long id, CancellationToken cancellationToken)
        {
            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return proposal;
        }

        private async Task<Opportunity> GetOpportunity(long opportunityId, CancellationToken cancellationToken)
        {
            Opportunity? opportunity = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == opportunityId, cancellationToken);

            if (opportunity is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return opportunity;
        }

        private async Task<Proposal> SaveAndReturn(Proposal proposal, CancellationToken cancellationToken)
        {
            bool success = await DbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetProposalById(proposal.Id, cancellationToken) ?? proposal;
        }

        private IQueryable<Proposal> QueryWithDetails()
        {
            return DbContext.Set<Proposal>()
                .AsNoTracking()
                .Include(item => item.Opportunity)
                    .ThenInclude(item => item.Brand)
                .Include(item => item.Campaign)
                .Include(item => item.Items)
                    .ThenInclude(item => item.Creator);
        }
    }
}
