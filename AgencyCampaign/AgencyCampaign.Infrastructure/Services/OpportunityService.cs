using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
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
        private readonly ICurrentUser currentUser;

        public OpportunityService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, ICurrentUser currentUser) : base(dbContext)
        {
            this.localizer = localizer;
            this.currentUser = currentUser;
        }

        public async Task<PagedResult<Opportunity>> GetOpportunities(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.ClosedAt.HasValue)
                .ThenBy(item => item.CommercialPipelineStage != null ? item.CommercialPipelineStage.DisplayOrder : int.MaxValue)
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
            CommercialPipelineStage initialStage = await ResolveInitialStage(request.CommercialPipelineStageId, cancellationToken);

            Opportunity opportunity = new(
                request.BrandId,
                initialStage.Id,
                request.Name,
                request.EstimatedValue,
                request.ExpectedCloseAt,
                request.Description,
                request.CommercialResponsibleId,
                request.ContactName,
                request.ContactEmail,
                request.Notes,
                currentUser.UserId,
                currentUser.UserName);

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
            CommercialPipelineStage stage = await ResolveStage(request.CommercialPipelineStageId ?? opportunity.CommercialPipelineStageId, cancellationToken);

            opportunity.Update(
                request.BrandId,
                request.Name,
                request.EstimatedValue,
                request.ExpectedCloseAt,
                request.Description,
                request.CommercialResponsibleId,
                request.ContactName,
                request.ContactEmail,
                request.Notes);

            opportunity.ChangeStage(stage, currentUser.UserId, currentUser.UserName);

            if (request.Probability.HasValue)
            {
                opportunity.SetProbability(request.Probability.Value);
            }

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
            CommercialPipelineStage stage = await ResolveStage(request.CommercialPipelineStageId, cancellationToken);
            opportunity.ChangeStage(stage, currentUser.UserId, currentUser.UserName, request.Reason);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public async Task<Opportunity> CloseAsWon(long id, CloseOpportunityAsWonRequest request, CancellationToken cancellationToken = default)
        {
            Opportunity opportunity = await GetTrackedOpportunity(id, cancellationToken);
            CommercialPipelineStage wonStage = await ResolveFinalStage(CommercialPipelineStageFinalBehavior.Won, cancellationToken);
            opportunity.CloseAsWon(wonStage, request.WonNotes, currentUser.UserId, currentUser.UserName);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public async Task<Opportunity> CloseAsLost(long id, CloseOpportunityAsLostRequest request, CancellationToken cancellationToken = default)
        {
            Opportunity opportunity = await GetTrackedOpportunity(id, cancellationToken);
            CommercialPipelineStage lostStage = await ResolveFinalStage(CommercialPipelineStageFinalBehavior.Lost, cancellationToken);
            opportunity.CloseAsLost(lostStage, request.LossReason, currentUser.UserId, currentUser.UserName);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public async Task<IReadOnlyCollection<OpportunityBoardStageModel>> GetBoard(CancellationToken cancellationToken = default)
        {
            List<Opportunity> opportunities = await QueryWithDetails()
                .OrderBy(item => item.CommercialPipelineStage != null ? item.CommercialPipelineStage.DisplayOrder : int.MaxValue)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);

            List<CommercialPipelineStage> stages = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);

            return stages
                .Select(stage =>
                {
                    List<OpportunityBoardItemModel> items = opportunities
                        .Where(item => item.CommercialPipelineStageId == stage.Id)
                        .Select(item => new OpportunityBoardItemModel
                        {
                            Id = item.Id,
                            BrandId = item.BrandId,
                            BrandName = item.Brand?.Name ?? string.Empty,
                            Name = item.Name,
                            CommercialPipelineStageId = stage.Id,
                            CommercialPipelineStageName = stage.Name,
                            CommercialPipelineStageColor = stage.Color,
                            EstimatedValue = item.EstimatedValue,
                            ExpectedCloseAt = item.ExpectedCloseAt,
                            CommercialResponsibleName = item.CommercialResponsible != null ? item.CommercialResponsible.Name : null,
                            ProposalCount = item.Proposals.Count,
                            PendingFollowUpsCount = item.FollowUps.Count(followUp => !followUp.IsCompleted),
                            OverdueFollowUpsCount = item.FollowUps.Count(followUp => !followUp.IsCompleted && followUp.DueAt < DateTimeOffset.UtcNow),
                            UpdatedAt = item.UpdatedAt
                        })
                        .ToList();

                    return new OpportunityBoardStageModel
                    {
                        CommercialPipelineStageId = stage.Id,
                        Name = stage.Name,
                        Color = stage.Color,
                        Description = stage.Description,
                        DisplayOrder = stage.DisplayOrder,
                        IsFinal = stage.IsFinal,
                        FinalBehavior = (int)stage.FinalBehavior,
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
                .Include(item => item.CommercialPipelineStage)
                .ToListAsync(cancellationToken);

            List<OpportunityNegotiation> negotiations = await DbContext.Set<OpportunityNegotiation>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<OpportunityFollowUp> followUps = await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            bool IsOpen(Opportunity item) => !item.ClosedAt.HasValue && item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.None;
            bool IsWon(Opportunity item) => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Won;
            bool IsLost(Opportunity item) => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost;

            return new CommercialDashboardSummaryModel
            {
                TotalOpportunities = opportunities.Count,
                OpenOpportunities = opportunities.Count(IsOpen),
                WonOpportunities = opportunities.Count(IsWon),
                LostOpportunities = opportunities.Count(IsLost),
                NegotiationsCount = negotiations.Count,
                PendingFollowUpsCount = followUps.Count(item => !item.IsCompleted),
                OverdueFollowUpsCount = followUps.Count(item => !item.IsCompleted && item.DueAt < now),
                TotalPipelineValue = opportunities.Where(IsOpen).Sum(item => item.EstimatedValue),
                WonValue = opportunities.Where(IsWon).Sum(item => item.EstimatedValue)
            };
        }

        public async Task<IReadOnlyCollection<CommercialFunnelStageModel>> GetFunnelConversion(CancellationToken cancellationToken = default)
        {
            List<CommercialPipelineStage> stages = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);

            List<Opportunity> opportunities = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .ToListAsync(cancellationToken);

            var enteredByStage = await DbContext.Set<OpportunityStageHistory>()
                .AsNoTracking()
                .GroupBy(item => item.ToStageId)
                .Select(group => new { StageId = group.Key, Count = group.Select(item => item.OpportunityId).Distinct().Count() })
                .ToDictionaryAsync(item => item.StageId, item => item.Count, cancellationToken);

            List<CommercialFunnelStageModel> result = [];
            int totalEntered = enteredByStage.Values.DefaultIfEmpty(0).Max();

            for (int index = 0; index < stages.Count; index++)
            {
                CommercialPipelineStage stage = stages[index];
                List<Opportunity> openInStage = opportunities
                    .Where(item => item.CommercialPipelineStageId == stage.Id && !item.ClosedAt.HasValue
                        && stage.FinalBehavior == CommercialPipelineStageFinalBehavior.None)
                    .ToList();

                int entered = enteredByStage.TryGetValue(stage.Id, out int count) ? count : 0;
                decimal conversion = totalEntered > 0
                    ? decimal.Round((decimal)entered / totalEntered * 100m, 1)
                    : 0m;

                result.Add(new CommercialFunnelStageModel
                {
                    StageId = stage.Id,
                    Name = stage.Name,
                    Color = stage.Color,
                    DisplayOrder = stage.DisplayOrder,
                    IsFinalBehavior = (int)stage.FinalBehavior,
                    OpenCount = openInStage.Count,
                    OpenValue = openInStage.Sum(item => item.EstimatedValue),
                    EnteredCount = entered,
                    ConversionRate = conversion
                });
            }

            return result;
        }

        public async Task<IReadOnlyCollection<CommercialResponsibleRankingModel>> GetResponsibleRanking(CancellationToken cancellationToken = default)
        {
            List<Opportunity> opportunities = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .Include(item => item.CommercialResponsible)
                .ToListAsync(cancellationToken);

            var grouped = opportunities
                .GroupBy(item => new { item.CommercialResponsibleId, Name = item.CommercialResponsible?.Name ?? "Sem responsável" });

            List<CommercialResponsibleRankingModel> result = [];

            foreach (var group in grouped)
            {
                List<Opportunity> open = group.Where(item => !item.ClosedAt.HasValue
                    && item.CommercialPipelineStage != null
                    && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.None).ToList();

                List<Opportunity> won = group.Where(item => item.CommercialPipelineStage != null
                    && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.Won).ToList();

                List<Opportunity> lost = group.Where(item => item.CommercialPipelineStage != null
                    && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost).ToList();

                int closedTotal = won.Count + lost.Count;
                decimal winRate = closedTotal > 0 ? decimal.Round((decimal)won.Count / closedTotal * 100m, 1) : 0m;

                result.Add(new CommercialResponsibleRankingModel
                {
                    CommercialResponsibleId = group.Key.CommercialResponsibleId,
                    Name = group.Key.Name,
                    OpenOpportunities = open.Count,
                    OpenValue = open.Sum(item => item.EstimatedValue),
                    WonOpportunities = won.Count,
                    WonValue = won.Sum(item => item.EstimatedValue),
                    LostOpportunities = lost.Count,
                    WinRate = winRate
                });
            }

            return result
                .OrderByDescending(item => item.WonValue)
                .ThenByDescending(item => item.OpenValue)
                .ToArray();
        }

        public async Task<CommercialForecastModel> GetForecast(DateTimeOffset fromMonth, DateTimeOffset toMonth, CancellationToken cancellationToken = default)
        {
            DateTimeOffset rangeStart = new DateTimeOffset(fromMonth.Year, fromMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset rangeEnd = new DateTimeOffset(toMonth.Year, toMonth.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);

            if (rangeEnd <= rangeStart)
            {
                throw new ArgumentException("toMonth must be greater than or equal to fromMonth.");
            }

            List<Opportunity> opportunities = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .Where(item => !item.ClosedAt.HasValue
                    && item.ExpectedCloseAt.HasValue
                    && item.ExpectedCloseAt >= rangeStart
                    && item.ExpectedCloseAt < rangeEnd
                    && item.CommercialPipelineStage != null
                    && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.None)
                .ToListAsync(cancellationToken);

            string[] monthLabels = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];
            List<CommercialForecastMonthModel> months = [];
            DateTimeOffset cursor = rangeStart;

            while (cursor < rangeEnd)
            {
                List<Opportunity> ofMonth = opportunities
                    .Where(item => item.ExpectedCloseAt!.Value.Year == cursor.Year && item.ExpectedCloseAt!.Value.Month == cursor.Month)
                    .ToList();

                decimal estimated = ofMonth.Sum(item => item.EstimatedValue);
                decimal weighted = ofMonth.Sum(item => item.EstimatedValue * (item.Probability / 100m));

                months.Add(new CommercialForecastMonthModel
                {
                    Month = $"{cursor.Year:D4}-{cursor.Month:D2}",
                    Label = $"{monthLabels[cursor.Month - 1]}/{cursor.Year % 100:D2}",
                    Estimated = decimal.Round(estimated, 2),
                    Weighted = decimal.Round(weighted, 2),
                    Count = ofMonth.Count
                });

                cursor = cursor.AddMonths(1);
            }

            return new CommercialForecastModel
            {
                Months = months,
                TotalEstimated = months.Sum(item => item.Estimated),
                TotalWeighted = months.Sum(item => item.Weighted),
                TotalCount = months.Sum(item => item.Count)
            };
        }

        public async Task<IReadOnlyCollection<OpportunityStageHistoryModel>> GetStageHistory(long opportunityId, CancellationToken cancellationToken = default)
        {
            bool exists = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == opportunityId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return await DbContext.Set<OpportunityStageHistory>()
                .AsNoTracking()
                .Include(item => item.FromStage)
                .Include(item => item.ToStage)
                .Where(item => item.OpportunityId == opportunityId)
                .OrderByDescending(item => item.ChangedAt)
                .Select(item => new OpportunityStageHistoryModel
                {
                    Id = item.Id,
                    OpportunityId = item.OpportunityId,
                    FromStageId = item.FromStageId,
                    FromStageName = item.FromStage != null ? item.FromStage.Name : null,
                    FromStageColor = item.FromStage != null ? item.FromStage.Color : null,
                    ToStageId = item.ToStageId,
                    ToStageName = item.ToStage != null ? item.ToStage.Name : string.Empty,
                    ToStageColor = item.ToStage != null ? item.ToStage.Color : null,
                    ChangedAt = item.ChangedAt,
                    ChangedByUserId = item.ChangedByUserId,
                    ChangedByUserName = item.ChangedByUserName,
                    Reason = item.Reason
                })
                .ToArrayAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<CommercialAlertModel>> GetAlerts(CancellationToken cancellationToken = default)
        {
            List<Opportunity> opportunities = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
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

            foreach (Opportunity opportunity in opportunities.Where(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.None && item.ExpectedCloseAt.HasValue && item.ExpectedCloseAt.Value < now))
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

        private async Task<CommercialPipelineStage> ResolveInitialStage(long? requestedStageId, CancellationToken cancellationToken)
        {
            if (requestedStageId.HasValue)
            {
                return await ResolveStage(requestedStageId.Value, cancellationToken);
            }

            CommercialPipelineStage? initialStage = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .OrderByDescending(item => item.IsInitial)
                .ThenBy(item => item.DisplayOrder)
                .FirstOrDefaultAsync(item => item.IsActive, cancellationToken);

            return initialStage ?? throw new InvalidOperationException("Nenhum estágio ativo foi configurado para o pipeline comercial.");
        }

        private async Task<CommercialPipelineStage> ResolveFinalStage(CommercialPipelineStageFinalBehavior finalBehavior, CancellationToken cancellationToken)
        {
            CommercialPipelineStage? stage = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IsActive && item.IsFinal && item.FinalBehavior == finalBehavior, cancellationToken);

            return stage ?? throw new InvalidOperationException("Nenhum estágio final configurado foi encontrado para o pipeline comercial.");
        }

        private async Task<CommercialPipelineStage> ResolveStage(long stageId, CancellationToken cancellationToken)
        {
            CommercialPipelineStage? stage = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == stageId, cancellationToken);

            return stage ?? throw new InvalidOperationException(localizer["record.notFound"]);
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
                .Include(item => item.CommercialPipelineStage)
                .Include(item => item.CommercialResponsible)
                .Include(item => item.Negotiations)
                    .ThenInclude(item => item.ApprovalRequests)
                .Include(item => item.FollowUps)
                .Include(item => item.Proposals);
        }
    }
}
