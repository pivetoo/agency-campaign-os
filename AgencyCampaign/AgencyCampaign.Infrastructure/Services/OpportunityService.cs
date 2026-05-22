using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Core.Pagination;
using Archon.Infrastructure.IdentityManagement;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityService : CrudService<Opportunity>, IOpportunityService
    {
        private readonly ICurrentUser currentUser;
        private readonly IdentityUsersClient identityUsersClient;

        public OpportunityService(DbContext dbContext, ICurrentUser currentUser, IdentityUsersClient identityUsersClient) : base(dbContext)
        {
            this.currentUser = currentUser;
            this.identityUsersClient = identityUsersClient;
        }

        private async Task<string?> ResolveResponsibleUserName(long? userId, CancellationToken cancellationToken)
        {
            if (!userId.HasValue) return null;
            try
            {
                IdentityUserDto? user = await identityUsersClient.GetUserByIdAsync(userId.Value, cancellationToken);
                return user?.Name;
            }
            catch
            {
                return null;
            }
        }

        public async Task<PagedResult<Opportunity>> GetOpportunities(PagedRequest request, OpportunityListFilters filters, CancellationToken cancellationToken = default)
        {
            return await GetOpportunitiesScoped(request, filters, restrictToCurrentUser: false, cancellationToken);
        }

        public async Task<PagedResult<Opportunity>> GetOpportunitiesScoped(PagedRequest request, OpportunityListFilters filters, bool restrictToCurrentUser, CancellationToken cancellationToken = default)
        {
            IQueryable<Opportunity> query = QueryWithDetails();
            query = ApplyOpportunityFilters(query, filters);

            if (restrictToCurrentUser)
            {
                long? userId = currentUser.UserId;
                if (!userId.HasValue)
                {
                    return new PagedResult<Opportunity> { Items = [] };
                }

                query = query.Where(item => item.ResponsibleUserId == userId.Value);
            }

            return await query
                .OrderBy(item => item.ClosedAt.HasValue)
                .ThenBy(item => item.CommercialPipelineStage != null ? item.CommercialPipelineStage.DisplayOrder : int.MaxValue)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        private static IQueryable<Opportunity> ApplyOpportunityFilters(IQueryable<Opportunity> query, OpportunityListFilters filters)
        {
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                string term = filters.Search.Trim().ToLower();
                query = query.Where(item =>
                    item.Name.ToLower().Contains(term)
                    || (item.Brand != null && item.Brand.Name.ToLower().Contains(term))
                    || (item.ContactName != null && item.ContactName.ToLower().Contains(term))
                    || (item.ContactEmail != null && item.ContactEmail.ToLower().Contains(term)));
            }

            if (filters.BrandId.HasValue)
            {
                query = query.Where(item => item.BrandId == filters.BrandId.Value);
            }

            if (filters.CommercialPipelineStageId.HasValue)
            {
                query = query.Where(item => item.CommercialPipelineStageId == filters.CommercialPipelineStageId.Value);
            }

            if (filters.ResponsibleUserId.HasValue)
            {
                query = query.Where(item => item.ResponsibleUserId == filters.ResponsibleUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Status))
            {
                string normalized = filters.Status.Trim().ToLowerInvariant();
                if (normalized == "open")
                {
                    query = query.Where(item => !item.ClosedAt.HasValue
                        && item.CommercialPipelineStage != null
                        && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.None);
                }
                else if (normalized == "won")
                {
                    query = query.Where(item => item.CommercialPipelineStage != null
                        && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.Won);
                }
                else if (normalized == "lost")
                {
                    query = query.Where(item => item.CommercialPipelineStage != null
                        && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost);
                }
            }

            if (filters.MinValue.HasValue)
            {
                query = query.Where(item => item.EstimatedValue >= filters.MinValue.Value);
            }

            if (filters.MaxValue.HasValue)
            {
                query = query.Where(item => item.EstimatedValue <= filters.MaxValue.Value);
            }

            if (filters.OpportunitySourceId.HasValue)
            {
                query = query.Where(item => item.OpportunitySourceId == filters.OpportunitySourceId.Value);
            }

            if (filters.OpportunityTagId.HasValue)
            {
                long tagId = filters.OpportunityTagId.Value;
                query = query.Where(item => item.TagAssignments.Any(assignment => assignment.OpportunityTagId == tagId));
            }

            return query;
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

            string? responsibleUserName = await ResolveResponsibleUserName(request.ResponsibleUserId, cancellationToken);

            Opportunity opportunity = new(
                request.BrandId,
                initialStage.Id,
                request.Name,
                request.EstimatedValue,
                request.ExpectedCloseAt,
                request.Description,
                request.ResponsibleUserId,
                responsibleUserName,
                request.ContactName,
                request.ContactEmail,
                request.Notes,
                currentUser.UserId,
                currentUser.UserName,
                request.ContactPhone);

            if (request.OpportunitySourceId.HasValue)
            {
                opportunity.SetSource(request.OpportunitySourceId);
            }

            if (request.Probability.HasValue)
            {
                opportunity.SetProbability(request.Probability.Value);
            }

            if (request.TagIds is not null && request.TagIds.Count > 0)
            {
                opportunity.ReplaceTags(request.TagIds);
            }

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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            Opportunity? opportunity = await DbContext.Set<Opportunity>()
                .AsTracking()
                .Include(item => item.TagAssignments)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (opportunity is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            await EnsureBrandExists(request.BrandId, cancellationToken);
            CommercialPipelineStage stage = await ResolveStage(request.CommercialPipelineStageId ?? opportunity.CommercialPipelineStageId, cancellationToken);

            string? responsibleUserName = await ResolveResponsibleUserName(request.ResponsibleUserId, cancellationToken);

            opportunity.Update(
                request.BrandId,
                request.Name,
                request.EstimatedValue,
                request.ExpectedCloseAt,
                request.Description,
                request.ResponsibleUserId,
                responsibleUserName,
                request.ContactName,
                request.ContactEmail,
                request.Notes,
                request.ContactPhone);

            opportunity.ChangeStage(stage, currentUser.UserId, currentUser.UserName);

            if (request.Probability.HasValue)
            {
                opportunity.SetProbability(request.Probability.Value);
            }

            opportunity.SetSource(request.OpportunitySourceId);

            if (request.TagIds is not null)
            {
                opportunity.ReplaceTags(request.TagIds);
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
            opportunity.CloseAsWon(wonStage, request.WonNotes, request.WinReasonId, currentUser.UserId, currentUser.UserName);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public async Task<Opportunity> CloseAsLost(long id, CloseOpportunityAsLostRequest request, CancellationToken cancellationToken = default)
        {
            Opportunity opportunity = await GetTrackedOpportunity(id, cancellationToken);
            CommercialPipelineStage lostStage = await ResolveFinalStage(CommercialPipelineStageFinalBehavior.Lost, cancellationToken);
            opportunity.CloseAsLost(lostStage, request.LossReason, request.LossReasonId, currentUser.UserId, currentUser.UserName);

            return await SaveAndReturn(opportunity, cancellationToken);
        }

        public Task<IReadOnlyCollection<OpportunityBoardStageModel>> GetBoard(CancellationToken cancellationToken = default)
        {
            return GetBoardScoped(restrictToCurrentUser: false, cancellationToken);
        }

        public async Task<IReadOnlyCollection<OpportunityBoardStageModel>> GetBoardScoped(bool restrictToCurrentUser, CancellationToken cancellationToken = default)
        {
            IQueryable<Opportunity> opportunitiesQuery = QueryWithDetails();
            if (restrictToCurrentUser)
            {
                long? userId = currentUser.UserId;
                if (!userId.HasValue)
                {
                    return [];
                }

                opportunitiesQuery = opportunitiesQuery.Where(item => item.ResponsibleUserId == userId.Value);
            }

            List<Opportunity> opportunities = await opportunitiesQuery
                .OrderBy(item => item.CommercialPipelineStage != null ? item.CommercialPipelineStage.DisplayOrder : int.MaxValue)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);

            List<CommercialPipelineStage> stages = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);

            long[] opportunityIds = opportunities.Select(item => item.Id).ToArray();

            Dictionary<long, DateTimeOffset> stageEnteredAtByOpportunity = await DbContext.Set<OpportunityStageHistory>()
                .AsNoTracking()
                .Where(history => opportunityIds.Contains(history.OpportunityId))
                .GroupBy(history => history.OpportunityId)
                .Select(group => new
                {
                    OpportunityId = group.Key,
                    LastChangedAt = group.Max(history => history.ChangedAt)
                })
                .ToDictionaryAsync(item => item.OpportunityId, item => item.LastChangedAt, cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            return stages
                .Select(stage =>
                {
                    int? slaInDays = stage.SlaInDays;

                    List<OpportunityBoardItemModel> items = opportunities
                        .Where(item => item.CommercialPipelineStageId == stage.Id)
                        .Select(item =>
                        {
                            DateTimeOffset stageEnteredAt = stageEnteredAtByOpportunity.TryGetValue(item.Id, out DateTimeOffset entered)
                                ? entered
                                : item.CreatedAt;

                            int daysInStage = (int)Math.Floor((now - stageEnteredAt).TotalDays);

                            string slaStatus;
                            if (!slaInDays.HasValue || stage.FinalBehavior != CommercialPipelineStageFinalBehavior.None)
                            {
                                slaStatus = "ok";
                            }
                            else if (daysInStage >= slaInDays.Value)
                            {
                                slaStatus = "breached";
                            }
                            else if (daysInStage >= Math.Max(slaInDays.Value - 2, 1))
                            {
                                slaStatus = "warning";
                            }
                            else
                            {
                                slaStatus = "ok";
                            }

                            return new OpportunityBoardItemModel
                            {
                                Id = item.Id,
                                BrandId = item.BrandId,
                                BrandName = item.Brand?.Name ?? string.Empty,
                                BrandLogoUrl = item.Brand?.LogoUrl,
                                Name = item.Name,
                                CommercialPipelineStageId = stage.Id,
                                CommercialPipelineStageName = stage.Name,
                                CommercialPipelineStageColor = stage.Color,
                                EstimatedValue = item.EstimatedValue,
                                ExpectedCloseAt = item.ExpectedCloseAt,
                                CommercialResponsibleName = item.ResponsibleUserName,
                                ProposalCount = item.Proposals.Count,
                                PendingFollowUpsCount = item.FollowUps.Count(followUp => !followUp.IsCompleted),
                                OverdueFollowUpsCount = item.FollowUps.Count(followUp => !followUp.IsCompleted && followUp.DueAt < now),
                                UpdatedAt = item.UpdatedAt,
                                StageEnteredAt = stageEnteredAt,
                                StageSlaInDays = slaInDays,
                                DaysInStage = daysInStage,
                                SlaStatus = slaStatus
                            };
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
                        SlaInDays = slaInDays,
                        OpportunitiesCount = items.Count,
                        EstimatedValueTotal = items.Sum(item => item.EstimatedValue),
                        Items = items
                    };
                })
                .ToArray();
        }

        public async Task<CommercialForecastModel> GetForecast(DateTimeOffset periodStart, DateTimeOffset periodEnd, bool restrictToCurrentUser, long? userId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset start = periodStart.ToUniversalTime();
            DateTimeOffset end = periodEnd.ToUniversalTime();

            long? scopeUserId = restrictToCurrentUser ? currentUser.UserId : userId;

            IQueryable<Opportunity> query = DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .Where(item => item.ExpectedCloseAt.HasValue && item.ExpectedCloseAt.Value >= start && item.ExpectedCloseAt.Value < end);

            if (scopeUserId.HasValue)
            {
                long target = scopeUserId.Value;
                query = query.Where(item => item.ResponsibleUserId == target);
            }

            List<Opportunity> all = await query.ToListAsync(cancellationToken);

            decimal weightedTotal = 0m;
            decimal unweightedTotal = 0m;
            decimal wonTotal = 0m;
            decimal lostTotal = 0m;
            int openCount = 0;
            int wonCount = 0;
            int lostCount = 0;

            foreach (Opportunity opportunity in all)
            {
                CommercialPipelineStageFinalBehavior behavior = opportunity.CommercialPipelineStage?.FinalBehavior ?? CommercialPipelineStageFinalBehavior.None;
                if (behavior == CommercialPipelineStageFinalBehavior.Won)
                {
                    wonTotal += opportunity.EstimatedValue;
                    wonCount++;
                    continue;
                }

                if (behavior == CommercialPipelineStageFinalBehavior.Lost)
                {
                    lostTotal += opportunity.EstimatedValue;
                    lostCount++;
                    continue;
                }

                openCount++;
                unweightedTotal += opportunity.EstimatedValue;
                weightedTotal += opportunity.EstimatedValue * (opportunity.Probability / 100m);
            }

            CommercialForecastStageBreakdown[] byStage = all
                .Where(item => item.CommercialPipelineStage != null
                    && item.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.None)
                .GroupBy(item => item.CommercialPipelineStage!)
                .OrderBy(group => group.Key.DisplayOrder)
                .Select(group => new CommercialForecastStageBreakdown
                {
                    StageId = group.Key.Id,
                    StageName = group.Key.Name,
                    StageColor = group.Key.Color,
                    Count = group.Count(),
                    TotalValue = group.Sum(item => item.EstimatedValue),
                    WeightedValue = group.Sum(item => item.EstimatedValue * (item.Probability / 100m)),
                    AverageProbability = group.Average(item => item.Probability)
                })
                .ToArray();

            return new CommercialForecastModel
            {
                PeriodStart = start,
                PeriodEnd = end,
                UserId = scopeUserId,
                WeightedTotal = Math.Round(weightedTotal, 2),
                UnweightedTotal = Math.Round(unweightedTotal, 2),
                WonTotal = Math.Round(wonTotal, 2),
                LostTotal = Math.Round(lostTotal, 2),
                OpenCount = openCount,
                WonCount = wonCount,
                LostCount = lostCount,
                ByStage = byStage
            };
        }

        public async Task<CommercialAnalyticsModel> GetAnalytics(DateTimeOffset periodStart, DateTimeOffset periodEnd, bool restrictToCurrentUser, long? userId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset start = periodStart.ToUniversalTime();
            DateTimeOffset end = periodEnd.ToUniversalTime();
            long? scopeUserId = restrictToCurrentUser ? currentUser.UserId : userId;

            List<CommercialPipelineStage> stages = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .OrderBy(stage => stage.DisplayOrder)
                .ToListAsync(cancellationToken);

            Dictionary<long, CommercialPipelineStage> stagesById = stages.ToDictionary(stage => stage.Id);
            long[] openStageIds = stages
                .Where(stage => stage.FinalBehavior == CommercialPipelineStageFinalBehavior.None)
                .Select(stage => stage.Id)
                .ToArray();

            IQueryable<Opportunity> closedQuery = DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .Where(item => item.ClosedAt.HasValue && item.ClosedAt.Value >= start && item.ClosedAt.Value < end);

            if (scopeUserId.HasValue)
            {
                long target = scopeUserId.Value;
                closedQuery = closedQuery.Where(item => item.ResponsibleUserId == target);
            }

            List<Opportunity> closed = await closedQuery.ToListAsync(cancellationToken);
            int wonCount = closed.Count(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Won);
            int lostCount = closed.Count(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost);
            int closedCount = closed.Count;
            decimal winRate = closedCount > 0 ? Math.Round((decimal)wonCount / closedCount * 100m, 2) : 0m;

            decimal averageCycleDays = 0m;
            if (closed.Count > 0)
            {
                averageCycleDays = (decimal)closed
                    .Select(item => (item.ClosedAt!.Value - item.CreatedAt).TotalDays)
                    .DefaultIfEmpty(0)
                    .Average();
                averageCycleDays = Math.Round(averageCycleDays, 1);
            }

            long[] closedIds = closed.Select(item => item.Id).ToArray();
            long[] currentOpenIds = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Where(item => !item.ClosedAt.HasValue
                    && (!scopeUserId.HasValue || item.ResponsibleUserId == scopeUserId.Value))
                .Select(item => item.Id)
                .ToArrayAsync(cancellationToken);
            long[] relevantOpportunityIds = closedIds.Concat(currentOpenIds).Distinct().ToArray();

            List<OpportunityStageHistory> history = await DbContext.Set<OpportunityStageHistory>()
                .AsNoTracking()
                .Where(item => relevantOpportunityIds.Contains(item.OpportunityId))
                .OrderBy(item => item.OpportunityId)
                .ThenBy(item => item.ChangedAt)
                .ToListAsync(cancellationToken);

            List<Opportunity> openSnapshot = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .Where(item => currentOpenIds.Contains(item.Id))
                .ToListAsync(cancellationToken);
            Dictionary<long, long> openCurrentStageById = openSnapshot.ToDictionary(item => item.Id, item => item.CommercialPipelineStageId);

            StageConversionModel[] conversionByStage = BuildConversionByStage(stages, history, closed, openCurrentStageById);
            StageTimeModel[] averageTimeInStage = BuildAverageTimeInStage(stages, history, closed, openCurrentStageById);

            ReasonAggregateModel[] winReasons = await BuildWinReasons(closed, cancellationToken);
            ReasonAggregateModel[] lossReasons = await BuildLossReasons(closed, cancellationToken);

            PerformerModel[] topPerformers = await BuildTopPerformers(closed, cancellationToken);

            return new CommercialAnalyticsModel
            {
                PeriodStart = start,
                PeriodEnd = end,
                UserId = scopeUserId,
                ClosedCount = closedCount,
                WonCount = wonCount,
                LostCount = lostCount,
                WinRate = winRate,
                AverageCycleDays = averageCycleDays,
                ConversionByStage = conversionByStage,
                AverageTimeInStage = averageTimeInStage,
                WinReasons = winReasons,
                LossReasons = lossReasons,
                TopPerformers = topPerformers
            };
        }

        private static StageConversionModel[] BuildConversionByStage(
            List<CommercialPipelineStage> stages,
            List<OpportunityStageHistory> history,
            List<Opportunity> closed,
            Dictionary<long, long> openCurrentStageById)
        {
            List<CommercialPipelineStage> openStages = stages
                .Where(stage => stage.FinalBehavior == CommercialPipelineStageFinalBehavior.None)
                .OrderBy(stage => stage.DisplayOrder)
                .ToList();

            Dictionary<long, CommercialPipelineStageFinalBehavior> stageBehavior = stages.ToDictionary(stage => stage.Id, stage => stage.FinalBehavior);
            Dictionary<long, int> stageOrder = stages.ToDictionary(stage => stage.Id, stage => stage.DisplayOrder);
            HashSet<long> wonOpportunityIds = closed
                .Where(opp => opp.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Won)
                .Select(opp => opp.Id)
                .ToHashSet();
            HashSet<long> lostOpportunityIds = closed
                .Where(opp => opp.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost)
                .Select(opp => opp.Id)
                .ToHashSet();

            Dictionary<long, List<long>> stagesVisitedByOpportunity = history
                .GroupBy(item => item.OpportunityId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.ToStageId).Distinct().ToList());

            List<StageConversionModel> result = new(openStages.Count);
            foreach (CommercialPipelineStage stage in openStages)
            {
                long stageId = stage.Id;
                int stageDisplayOrder = stage.DisplayOrder;
                List<long> enteredOpportunities = stagesVisitedByOpportunity
                    .Where(pair => pair.Value.Contains(stageId))
                    .Select(pair => pair.Key)
                    .ToList();

                int entered = enteredOpportunities.Count;
                int advanced = 0;
                int stuck = 0;
                int lost = 0;

                foreach (long opportunityId in enteredOpportunities)
                {
                    if (wonOpportunityIds.Contains(opportunityId))
                    {
                        advanced++;
                        continue;
                    }

                    if (lostOpportunityIds.Contains(opportunityId))
                    {
                        lost++;
                        continue;
                    }

                    if (openCurrentStageById.TryGetValue(opportunityId, out long currentStageId))
                    {
                        if (currentStageId == stageId)
                        {
                            stuck++;
                            continue;
                        }

                        if (stageOrder.TryGetValue(currentStageId, out int currentOrder) && currentOrder > stageDisplayOrder)
                        {
                            advanced++;
                            continue;
                        }

                        if (stageBehavior.TryGetValue(currentStageId, out CommercialPipelineStageFinalBehavior behavior))
                        {
                            if (behavior == CommercialPipelineStageFinalBehavior.Won)
                            {
                                advanced++;
                                continue;
                            }
                            if (behavior == CommercialPipelineStageFinalBehavior.Lost)
                            {
                                lost++;
                                continue;
                            }
                        }
                    }
                }

                decimal conversionRate = entered > 0 ? Math.Round((decimal)advanced / entered * 100m, 2) : 0m;

                result.Add(new StageConversionModel
                {
                    StageId = stage.Id,
                    StageName = stage.Name,
                    StageColor = stage.Color,
                    DisplayOrder = stage.DisplayOrder,
                    Entered = entered,
                    Advanced = advanced,
                    Stuck = stuck,
                    Lost = lost,
                    ConversionRate = conversionRate
                });
            }

            return [.. result];
        }

        private static StageTimeModel[] BuildAverageTimeInStage(
            List<CommercialPipelineStage> stages,
            List<OpportunityStageHistory> history,
            List<Opportunity> closed,
            Dictionary<long, long> openCurrentStageById)
        {
            Dictionary<long, DateTimeOffset> closedAtByOpportunity = closed
                .Where(item => item.ClosedAt.HasValue)
                .ToDictionary(item => item.Id, item => item.ClosedAt!.Value);

            Dictionary<long, List<(decimal days, long stageId)>> durationsByStage = stages.ToDictionary(stage => stage.Id, _ => new List<(decimal, long)>());

            foreach (IGrouping<long, OpportunityStageHistory> group in history.GroupBy(item => item.OpportunityId))
            {
                List<OpportunityStageHistory> ordered = group.OrderBy(item => item.ChangedAt).ToList();
                for (int index = 0; index < ordered.Count; index++)
                {
                    long stageId = ordered[index].ToStageId;
                    DateTimeOffset enteredAt = ordered[index].ChangedAt;
                    DateTimeOffset leftAt;
                    if (index + 1 < ordered.Count)
                    {
                        leftAt = ordered[index + 1].ChangedAt;
                    }
                    else if (closedAtByOpportunity.TryGetValue(group.Key, out DateTimeOffset closedAt))
                    {
                        leftAt = closedAt;
                    }
                    else if (openCurrentStageById.TryGetValue(group.Key, out long currentStageId) && currentStageId == stageId)
                    {
                        leftAt = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        continue;
                    }

                    decimal days = (decimal)(leftAt - enteredAt).TotalDays;
                    if (days < 0)
                    {
                        days = 0;
                    }

                    if (durationsByStage.TryGetValue(stageId, out List<(decimal days, long stageId)>? bucket))
                    {
                        bucket.Add((days, stageId));
                    }
                }
            }

            return stages
                .Where(stage => stage.FinalBehavior == CommercialPipelineStageFinalBehavior.None)
                .OrderBy(stage => stage.DisplayOrder)
                .Select(stage =>
                {
                    List<(decimal days, long stageId)> bucket = durationsByStage[stage.Id];
                    decimal avg = bucket.Count > 0 ? Math.Round(bucket.Average(entry => entry.days), 1) : 0m;
                    return new StageTimeModel
                    {
                        StageId = stage.Id,
                        StageName = stage.Name,
                        StageColor = stage.Color,
                        DisplayOrder = stage.DisplayOrder,
                        AverageDays = avg,
                        Samples = bucket.Count
                    };
                })
                .ToArray();
        }

        private async Task<ReasonAggregateModel[]> BuildWinReasons(List<Opportunity> closed, CancellationToken cancellationToken)
        {
            List<Opportunity> won = closed.Where(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Won).ToList();
            if (won.Count == 0)
            {
                return [];
            }

            Dictionary<long, OpportunityWinReason> reasonsById = await DbContext.Set<OpportunityWinReason>()
                .AsNoTracking()
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            return won
                .GroupBy(item => item.WinReasonId)
                .Select(group =>
                {
                    string name = group.Key.HasValue && reasonsById.TryGetValue(group.Key.Value, out OpportunityWinReason? reason)
                        ? reason.Name
                        : "Sem motivo";
                    string? color = group.Key.HasValue && reasonsById.TryGetValue(group.Key.Value, out OpportunityWinReason? colorReason)
                        ? colorReason.Color
                        : null;
                    return new ReasonAggregateModel
                    {
                        ReasonId = group.Key,
                        ReasonName = name,
                        ReasonColor = color,
                        Count = group.Count(),
                        TotalValue = Math.Round(group.Sum(item => item.EstimatedValue), 2)
                    };
                })
                .OrderByDescending(item => item.Count)
                .ToArray();
        }

        private async Task<ReasonAggregateModel[]> BuildLossReasons(List<Opportunity> closed, CancellationToken cancellationToken)
        {
            List<Opportunity> lost = closed.Where(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost).ToList();
            if (lost.Count == 0)
            {
                return [];
            }

            Dictionary<long, OpportunityLossReason> reasonsById = await DbContext.Set<OpportunityLossReason>()
                .AsNoTracking()
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            return lost
                .GroupBy(item => item.LossReasonId)
                .Select(group =>
                {
                    string name = group.Key.HasValue && reasonsById.TryGetValue(group.Key.Value, out OpportunityLossReason? reason)
                        ? reason.Name
                        : "Sem motivo";
                    string? color = group.Key.HasValue && reasonsById.TryGetValue(group.Key.Value, out OpportunityLossReason? colorReason)
                        ? colorReason.Color
                        : null;
                    return new ReasonAggregateModel
                    {
                        ReasonId = group.Key,
                        ReasonName = name,
                        ReasonColor = color,
                        Count = group.Count(),
                        TotalValue = Math.Round(group.Sum(item => item.EstimatedValue), 2)
                    };
                })
                .OrderByDescending(item => item.Count)
                .ToArray();
        }

        private async Task<PerformerModel[]> BuildTopPerformers(List<Opportunity> closed, CancellationToken cancellationToken)
        {
            List<Opportunity> won = closed.Where(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Won).ToList();
            if (won.Count == 0)
            {
                return [];
            }

            var groups = won
                .GroupBy(item => item.ResponsibleUserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    WonCount = group.Count(),
                    WonTotal = Math.Round(group.Sum(item => item.EstimatedValue), 2)
                })
                .OrderByDescending(item => item.WonTotal)
                .Take(10)
                .ToArray();

            List<PerformerModel> result = new(groups.Length);
            foreach (var entry in groups)
            {
                string userName = "Sem responsável";
                if (entry.UserId.HasValue)
                {
                    userName = await ResolveResponsibleUserName(entry.UserId, cancellationToken) ?? $"Usuário #{entry.UserId.Value}";
                }
                result.Add(new PerformerModel
                {
                    UserId = entry.UserId,
                    UserName = userName,
                    WonCount = entry.WonCount,
                    WonTotal = entry.WonTotal
                });
            }

            return [.. result];
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

        public async Task<IReadOnlyCollection<OpportunityStageHistoryModel>> GetStageHistory(long opportunityId, CancellationToken cancellationToken = default)
        {
            bool exists = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == opportunityId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
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

            long[] openOpportunityIds = opportunities
                .Where(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.None)
                .Select(item => item.Id)
                .ToArray();

            if (openOpportunityIds.Length > 0)
            {
                Dictionary<long, DateTimeOffset> stageEnteredAtByOpportunity = await DbContext.Set<OpportunityStageHistory>()
                    .AsNoTracking()
                    .Where(history => openOpportunityIds.Contains(history.OpportunityId))
                    .GroupBy(history => history.OpportunityId)
                    .Select(group => new
                    {
                        OpportunityId = group.Key,
                        LastChangedAt = group.Max(history => history.ChangedAt)
                    })
                    .ToDictionaryAsync(item => item.OpportunityId, item => item.LastChangedAt, cancellationToken);

                foreach (Opportunity opportunity in opportunities.Where(item => openOpportunityIds.Contains(item.Id) && item.CommercialPipelineStage?.SlaInDays > 0))
                {
                    int slaInDays = opportunity.CommercialPipelineStage!.SlaInDays!.Value;
                    DateTimeOffset stageEnteredAt = stageEnteredAtByOpportunity.TryGetValue(opportunity.Id, out DateTimeOffset entered)
                        ? entered
                        : opportunity.CreatedAt;
                    int daysInStage = (int)Math.Floor((now - stageEnteredAt).TotalDays);

                    if (daysInStage < slaInDays)
                    {
                        continue;
                    }

                    alerts.Add(new CommercialAlertModel
                    {
                        Type = "stagesla",
                        Severity = "high",
                        Title = "SLA do estágio excedido",
                        Description = $"Há {daysInStage} dias em \"{opportunity.CommercialPipelineStage.Name}\" (SLA: {slaInDays} dias).",
                        OpportunityId = opportunity.Id,
                        OpportunityName = opportunity.Name,
                        DueAt = stageEnteredAt.AddDays(slaInDays)
                    });
                }
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
                throw new InvalidOperationException("record.notFound");
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

            return initialStage ?? throw new InvalidOperationException("commercialPipelineStage.initial.missing");
        }

        private async Task<CommercialPipelineStage> ResolveFinalStage(CommercialPipelineStageFinalBehavior finalBehavior, CancellationToken cancellationToken)
        {
            CommercialPipelineStage? stage = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IsActive && item.IsFinal && item.FinalBehavior == finalBehavior, cancellationToken);

            return stage ?? throw new InvalidOperationException("commercialPipelineStage.final.missing");
        }

        private async Task<CommercialPipelineStage> ResolveStage(long stageId, CancellationToken cancellationToken)
        {
            CommercialPipelineStage? stage = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == stageId, cancellationToken);

            return stage ?? throw new InvalidOperationException("record.notFound");
        }

        private async Task<Opportunity> GetTrackedOpportunity(long id, CancellationToken cancellationToken)
        {
            Opportunity? opportunity = await DbContext.Set<Opportunity>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (opportunity is null)
            {
                throw new InvalidOperationException("record.notFound");
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
                .Include(item => item.OpportunitySource)
                .Include(item => item.Negotiations)
                    .ThenInclude(item => item.ApprovalRequests)
                .Include(item => item.FollowUps)
                .Include(item => item.Proposals)
                .Include(item => item.TagAssignments)
                    .ThenInclude(item => item.OpportunityTag);
        }
    }
}
