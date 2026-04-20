using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityService : CrudService<Opportunity>, IOpportunityService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public OpportunityService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Opportunity>> GetOpportunities(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.Stage == OpportunityStage.Won || item.Stage == OpportunityStage.Lost)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Opportunity?> GetOpportunityById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Opportunity> CreateOpportunity(CreateOpportunityRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureBrandExists(request.BrandId, cancellationToken);

            Opportunity opportunity = new(
                request.BrandId,
                request.Name,
                request.EstimatedValue,
                request.ExpectedCloseAt,
                request.Description,
                request.InternalOwnerId,
                request.InternalOwnerName,
                request.ContactName,
                request.ContactEmail,
                request.Notes);

            bool success = await Insert(cancellationToken, opportunity);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityById(opportunity.Id, cancellationToken) ?? opportunity;
        }

        public async Task<Opportunity> UpdateOpportunity(long id, UpdateOpportunityRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Opportunity? opportunity = await DbContext.Set<Opportunity>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (opportunity is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureBrandExists(request.BrandId, cancellationToken);

            opportunity.Update(
                request.BrandId,
                request.Name,
                request.EstimatedValue,
                request.ExpectedCloseAt,
                request.Description,
                request.InternalOwnerId,
                request.InternalOwnerName,
                request.ContactName,
                request.ContactEmail,
                request.Notes);

            Opportunity? result = await Update(opportunity, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityById(result.Id, cancellationToken) ?? result;
        }

        public async Task<Opportunity> ChangeStage(long id, ChangeOpportunityStageRequest request, CancellationToken cancellationToken = default)
        {
            Opportunity opportunity = await GetTrackedOpportunity(id, cancellationToken);
            opportunity.ChangeStage(request.Stage);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public async Task<Opportunity> CloseAsWon(long id, CloseOpportunityAsWonRequest request, CancellationToken cancellationToken = default)
        {
            Opportunity opportunity = await GetTrackedOpportunity(id, cancellationToken);
            opportunity.CloseAsWon(request.WonNotes);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public async Task<Opportunity> CloseAsLost(long id, CloseOpportunityAsLostRequest request, CancellationToken cancellationToken = default)
        {
            Opportunity opportunity = await GetTrackedOpportunity(id, cancellationToken);
            opportunity.CloseAsLost(request.LossReason);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public async Task<IReadOnlyCollection<OpportunityBoardStageModel>> GetBoard(CancellationToken cancellationToken = default)
        {
            List<Opportunity> opportunities = await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);

            return Enum.GetValues<OpportunityStage>()
                .Select(stage =>
                {
                    List<OpportunityBoardItemModel> items = opportunities
                        .Where(item => item.Stage == stage)
                        .Select(item => new OpportunityBoardItemModel
                        {
                            Id = item.Id,
                            BrandId = item.BrandId,
                            BrandName = item.Brand?.Name ?? string.Empty,
                            Name = item.Name,
                            Stage = item.Stage,
                            EstimatedValue = item.EstimatedValue,
                            ExpectedCloseAt = item.ExpectedCloseAt,
                            InternalOwnerName = item.InternalOwnerName,
                            ProposalCount = item.Proposals.Count,
                            PendingFollowUpsCount = item.FollowUps.Count(followUp => !followUp.IsCompleted),
                            OverdueFollowUpsCount = item.FollowUps.Count(followUp => !followUp.IsCompleted && followUp.DueAt < DateTimeOffset.UtcNow),
                            UpdatedAt = item.UpdatedAt
                        })
                        .ToList();

                    return new OpportunityBoardStageModel
                    {
                        Stage = stage,
                        OpportunitiesCount = items.Count,
                        EstimatedValueTotal = items.Sum(item => item.EstimatedValue),
                        Items = items
                    };
                })
                .ToArray();
        }

        public async Task<CommercialDashboardSummaryModel> GetDashboardSummary(CancellationToken cancellationToken = default)
        {
            List<Opportunity> opportunities = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<OpportunityNegotiation> negotiations = await DbContext.Set<OpportunityNegotiation>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<OpportunityFollowUp> followUps = await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            OpportunityStage[] openStages = [OpportunityStage.Lead, OpportunityStage.Qualified, OpportunityStage.Proposal, OpportunityStage.Negotiation];

            return new CommercialDashboardSummaryModel
            {
                TotalOpportunities = opportunities.Count,
                OpenOpportunities = opportunities.Count(item => openStages.Contains(item.Stage)),
                WonOpportunities = opportunities.Count(item => item.Stage == OpportunityStage.Won),
                LostOpportunities = opportunities.Count(item => item.Stage == OpportunityStage.Lost),
                NegotiationsCount = negotiations.Count,
                PendingFollowUpsCount = followUps.Count(item => !item.IsCompleted),
                OverdueFollowUpsCount = followUps.Count(item => !item.IsCompleted && item.DueAt < now),
                TotalPipelineValue = opportunities.Where(item => openStages.Contains(item.Stage)).Sum(item => item.EstimatedValue),
                WonValue = opportunities.Where(item => item.Stage == OpportunityStage.Won).Sum(item => item.EstimatedValue)
            };
        }

        public async Task<IReadOnlyCollection<CommercialAlertModel>> GetAlerts(CancellationToken cancellationToken = default)
        {
            List<Opportunity> opportunities = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<OpportunityFollowUp> followUps = await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            List<CommercialAlertModel> alerts = [];

            foreach (OpportunityFollowUp followUp in followUps.Where(item => !item.IsCompleted))
            {
                Opportunity? opportunity = opportunities.FirstOrDefault(item => item.Id == followUp.OpportunityId);
                if (opportunity is null)
                {
                    continue;
                }

                bool isOverdue = followUp.DueAt < now;
                alerts.Add(new CommercialAlertModel
                {
                    Type = "followup",
                    Severity = isOverdue ? "high" : "medium",
                    Title = isOverdue ? "Follow-up atrasado" : "Follow-up pendente",
                    Description = followUp.Subject,
                    OpportunityId = opportunity.Id,
                    OpportunityName = opportunity.Name,
                    FollowUpId = followUp.Id,
                    DueAt = followUp.DueAt
                });
            }

            foreach (Opportunity opportunity in opportunities.Where(item => item.Stage != OpportunityStage.Won && item.Stage != OpportunityStage.Lost && item.ExpectedCloseAt.HasValue && item.ExpectedCloseAt.Value < now))
            {
                alerts.Add(new CommercialAlertModel
                {
                    Type = "expectedclose",
                    Severity = "medium",
                    Title = "Previsão de fechamento vencida",
                    Description = "A oportunidade passou da data prevista de fechamento.",
                    OpportunityId = opportunity.Id,
                    OpportunityName = opportunity.Name,
                    DueAt = opportunity.ExpectedCloseAt
                });
            }

            return alerts
                .OrderBy(item => item.DueAt ?? DateTimeOffset.MaxValue)
                .ToArray();
        }

        private async Task EnsureBrandExists(long brandId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Brand>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == brandId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task<Opportunity> GetTrackedOpportunity(long id, CancellationToken cancellationToken)
        {
            Opportunity? opportunity = await DbContext.Set<Opportunity>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (opportunity is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return opportunity;
        }

        private async Task<Opportunity> SaveAndReturn(Opportunity opportunity, CancellationToken cancellationToken)
        {
            bool success = await DbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityById(opportunity.Id, cancellationToken) ?? opportunity;
        }

        private IQueryable<Opportunity> QueryWithDetails()
        {
            return DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.Brand)
                .Include(item => item.Negotiations)
                    .ThenInclude(item => item.ApprovalRequests)
                .Include(item => item.FollowUps)
                .Include(item => item.Proposals);
        }
    }
}
