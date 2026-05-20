using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Application.Abstractions;
using Archon.Application.Integrations;
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
        private readonly ICurrentUser currentUser;
        private readonly IFinancialAutoGeneration financialAutoGeneration;
        private readonly IAutomationDispatcher automationDispatcher;
        private readonly INotificationService notificationService;
        private readonly IntegrationPlatformClient integrationPlatformClient;
        private readonly IAutomationService automationService;

        public ProposalService(DbContext dbContext, ICurrentUser currentUser, IFinancialAutoGeneration financialAutoGeneration, IAutomationDispatcher automationDispatcher, INotificationService notificationService, IntegrationPlatformClient integrationPlatformClient, IAutomationService automationService) : base(dbContext)
        {
            this.currentUser = currentUser;
            this.financialAutoGeneration = financialAutoGeneration;
            this.automationDispatcher = automationDispatcher;
            this.notificationService = notificationService;
            this.integrationPlatformClient = integrationPlatformClient;
            this.automationService = automationService;
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

            long responsibleUserId = request.ResponsibleUserId ?? opportunity.ResponsibleUserId ?? throw new InvalidOperationException("proposal.responsibleUser.required");
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

            if (request.ProposalLayoutId.HasValue)
            {
                proposal.SetProposalLayout(request.ProposalLayoutId);
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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            Opportunity opportunity = await GetOpportunity(request.OpportunityId, cancellationToken);

            proposal.Update(
                opportunity.Name,
                request.ValidityUntil,
                request.Description,
                request.Notes,
                request.OpportunityId);

            proposal.SetProposalLayout(request.ProposalLayoutId);

            Proposal? result = await Update(proposal, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetProposalById(result.Id, cancellationToken) ?? result;
        }

        public async Task<Proposal> MarkAsSent(long id, CancellationToken cancellationToken = default)
        {
            Proposal proposal = await CreateSentVersionAsync(id, cancellationToken);
            Proposal saved = await SaveAndReturn(proposal, cancellationToken);
            await NotifyAutomations(AutomationTriggers.ProposalSent, saved, cancellationToken);
            return saved;
        }

        public async Task<Proposal> SendByEmail(long id, SendProposalEmailRequest request, CancellationToken cancellationToken = default)
        {
            (long connectorId, long pipelineId) = await ResolveTargetsAsync(request.ConnectorId, request.PipelineId, IntegrationIntents.ProposalSendEmail, cancellationToken);

            Proposal proposal = await CreateSentVersionAsync(id, cancellationToken);

            ProposalShareLink shareLink = await EnsureActiveShareLinkAsync(id, cancellationToken);

            string payload = JsonSerializer.Serialize(new
            {
                proposalId = proposal.Id,
                proposalName = proposal.Name,
                recipientEmail = request.RecipientEmail,
                subject = request.Subject,
                body = request.Body,
                publicToken = shareLink.Token,
                totalValue = proposal.TotalValue,
                validityUntil = proposal.ValidityUntil,
                sentByUserName = currentUser.UserName
            });

            EnqueuePipelineRequest enqueueRequest = new()
            {
                ConnectorId = connectorId,
                PipelineId = pipelineId,
                Payload = payload,
                Priority = 1
            };

            await integrationPlatformClient.EnqueuePipelineAsync(enqueueRequest, cancellationToken);

            Proposal saved = await SaveAndReturn(proposal, cancellationToken);
            await NotifyAutomations(AutomationTriggers.ProposalSent, saved, cancellationToken);
            return saved;
        }

        public async Task<Proposal> SendByWhatsapp(long id, SendProposalWhatsappRequest request, CancellationToken cancellationToken = default)
        {
            (long connectorId, long pipelineId) = await ResolveTargetsAsync(request.ConnectorId, request.PipelineId, IntegrationIntents.ProposalSendWhatsapp, cancellationToken);

            Proposal proposal = await CreateSentVersionAsync(id, cancellationToken);

            ProposalShareLink shareLink = await EnsureActiveShareLinkAsync(id, cancellationToken);

            string payload = JsonSerializer.Serialize(new
            {
                proposalId = proposal.Id,
                proposalName = proposal.Name,
                recipientPhone = request.RecipientPhone,
                body = request.Body,
                publicToken = shareLink.Token,
                totalValue = proposal.TotalValue,
                validityUntil = proposal.ValidityUntil,
                sentByUserName = currentUser.UserName
            });

            EnqueuePipelineRequest enqueueRequest = new()
            {
                ConnectorId = connectorId,
                PipelineId = pipelineId,
                Payload = payload,
                Priority = 1
            };

            await integrationPlatformClient.EnqueuePipelineAsync(enqueueRequest, cancellationToken);

            Proposal saved = await SaveAndReturn(proposal, cancellationToken);
            await NotifyAutomations(AutomationTriggers.ProposalSent, saved, cancellationToken);
            return saved;
        }

        private async Task<(long ConnectorId, long PipelineId)> ResolveTargetsAsync(long? overrideConnectorId, long? overridePipelineId, string intentKey, CancellationToken cancellationToken)
        {
            if (overrideConnectorId is > 0 && overridePipelineId is > 0)
            {
                return (overrideConnectorId.Value, overridePipelineId.Value);
            }

            Automation? defaultAutomation = await automationService.GetDefaultForUserAction(intentKey, cancellationToken);
            if (defaultAutomation is null)
            {
                throw new InvalidOperationException("automation.userAction.notConfigured");
            }

            return (defaultAutomation.ConnectorId, defaultAutomation.PipelineId);
        }

        private async Task<Proposal> CreateSentVersionAsync(long proposalId, CancellationToken cancellationToken)
        {
            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .Include(item => item.Items)
                    .ThenInclude(item => item.Creator)
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            int nextVersion = await DbContext.Set<ProposalVersion>()
                .Where(item => item.ProposalId == proposalId)
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

            return proposal;
        }

        private async Task<ProposalShareLink> EnsureActiveShareLinkAsync(long proposalId, CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            ProposalShareLink? active = await DbContext.Set<ProposalShareLink>()
                .AsTracking()
                .Where(item => item.ProposalId == proposalId
                    && item.RevokedAt == null
                    && (item.ExpiresAt == null || item.ExpiresAt > now))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (active is not null)
            {
                return active;
            }

            string token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", string.Empty);

            ProposalShareLink created = new(proposalId, token, null, currentUser.UserId, currentUser.UserName);
            DbContext.Set<ProposalShareLink>().Add(created);
            return created;
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
            await NotifyAutomations(AutomationTriggers.ProposalApproved, saved, cancellationToken);
            await TryNotify(KanvasNotifications.ProposalApproved(saved), cancellationToken);
            return saved;
        }

        public async Task<Proposal> RejectProposal(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.Reject(currentUser.UserId, currentUser.UserName);

            Proposal saved = await SaveAndReturn(proposal, cancellationToken);
            await NotifyAutomations(AutomationTriggers.ProposalRejected, saved, cancellationToken);
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
                throw new InvalidOperationException("record.notFound");
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

            await NotifyAutomations(AutomationTriggers.ProposalConverted, saved, cancellationToken);
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

        private async Task NotifyAutomations(string trigger, Proposal proposal, CancellationToken cancellationToken)
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

            try
            {
                await automationDispatcher.DispatchAsync(trigger, payload, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[ProposalService] failed to dispatch automation for {trigger}: {exception.Message}");
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
                throw new InvalidOperationException("record.notFound");
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
                throw new InvalidOperationException("record.notFound");
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
                throw new InvalidOperationException("record.notFound");
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
