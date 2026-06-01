using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Commercial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Exceptions;
using Archon.Core.Pagination;
using Archon.Infrastructure.IdentityManagement;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CommercialGoalService : ICommercialGoalService
    {
        private readonly DbContext dbContext;
        private readonly IdentityUsersClient identityUsersClient;

        public CommercialGoalService(DbContext dbContext, IdentityUsersClient identityUsersClient)
        {
            this.dbContext = dbContext;
            this.identityUsersClient = identityUsersClient;
        }

        public async Task<PagedResult<CommercialGoalModel>> GetAll(PagedRequest request, bool includeInactive, long? userId, int? periodType, CancellationToken cancellationToken = default)
        {
            IQueryable<CommercialGoal> query = dbContext.Set<CommercialGoal>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (userId.HasValue)
            {
                long target = userId.Value;
                query = query.Where(item => item.UserId == target);
            }

            if (periodType.HasValue)
            {
                CommercialGoalPeriodType type = (CommercialGoalPeriodType)periodType.Value;
                query = query.Where(item => item.PeriodType == type);
            }

            PagedResult<CommercialGoal> page = await query
                .OrderByDescending(item => item.PeriodStart)
                .ThenBy(item => item.UserId == null ? 0 : 1)
                .ToPagedResultAsync(request, cancellationToken);

            CommercialGoalModel[] items = await EnrichWithUserNames(page.Items, cancellationToken);
            return new PagedResult<CommercialGoalModel>
            {
                Items = items,
                Pagination = page.Pagination
            };
        }

        public async Task<CommercialGoalModel> Create(CreateCommercialGoalRequest request, CancellationToken cancellationToken = default)
        {
            CommercialGoal goal = new(request.UserId, (CommercialGoalPeriodType)request.PeriodType, request.PeriodStart, request.TargetAmount, request.Notes);

            long userKey = goal.UserId ?? 0;
            bool duplicate = await dbContext.Set<CommercialGoal>()
                .AsNoTracking()
                .AnyAsync(item => (item.UserId ?? 0) == userKey
                    && item.PeriodType == goal.PeriodType
                    && item.PeriodStart == goal.PeriodStart, cancellationToken);

            if (duplicate)
            {
                throw new ConflictException("commercialGoal.duplicate");
            }

            dbContext.Set<CommercialGoal>().Add(goal);
            await dbContext.SaveChangesAsync(cancellationToken);
            return (await EnrichWithUserNames([goal], cancellationToken))[0];
        }

        public async Task<CommercialGoalModel> Update(long id, UpdateCommercialGoalRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            CommercialGoal? goal = await dbContext.Set<CommercialGoal>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (goal is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            goal.Update(request.UserId, (CommercialGoalPeriodType)request.PeriodType, request.PeriodStart, request.TargetAmount, request.Notes, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return (await EnrichWithUserNames([goal], cancellationToken))[0];
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            CommercialGoal? goal = await dbContext.Set<CommercialGoal>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (goal is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            dbContext.Set<CommercialGoal>().Remove(goal);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<CommercialGoalProgressModel>> GetProgress(DateTimeOffset referenceDate, long? userId, int? periodType, CancellationToken cancellationToken = default)
        {
            DateTimeOffset utcReference = referenceDate.ToUniversalTime();

            IQueryable<CommercialGoal> query = dbContext.Set<CommercialGoal>()
                .AsNoTracking()
                .Where(item => item.IsActive);

            if (userId.HasValue)
            {
                long target = userId.Value;
                query = query.Where(item => item.UserId == target);
            }

            if (periodType.HasValue)
            {
                CommercialGoalPeriodType type = (CommercialGoalPeriodType)periodType.Value;
                query = query.Where(item => item.PeriodType == type);
            }

            List<CommercialGoal> goals = await query
                .Where(item => item.PeriodStart <= utcReference)
                .OrderBy(item => item.PeriodStart)
                .ToListAsync(cancellationToken);

            goals = goals.Where(goal => goal.PeriodEnd() > utcReference).ToList();

            if (goals.Count == 0)
            {
                return [];
            }

            CommercialGoalModel[] enriched = await EnrichWithUserNames(goals, cancellationToken);

            List<CommercialGoalProgressModel> result = new(goals.Count);
            for (int index = 0; index < goals.Count; index++)
            {
                CommercialGoal goal = goals[index];
                CommercialGoalModel meta = enriched[index];
                DateTimeOffset periodEnd = goal.PeriodEnd();

                IQueryable<Opportunity> won = dbContext.Set<Opportunity>()
                    .AsNoTracking()
                    .Where(opp => opp.ClosedAt.HasValue
                        && opp.ClosedAt.Value >= goal.PeriodStart
                        && opp.ClosedAt.Value < periodEnd
                        && opp.CommercialPipelineStage != null
                        && opp.CommercialPipelineStage.FinalBehavior == CommercialPipelineStageFinalBehavior.Won);

                if (goal.UserId.HasValue)
                {
                    long target = goal.UserId.Value;
                    won = won.Where(opp => opp.ResponsibleUserId == target);
                }

                decimal achievedAmount = await won.SumAsync(opp => (decimal?)(opp.ClosedValue ?? opp.EstimatedValue) ?? 0m, cancellationToken);
                int achievedCount = await won.CountAsync(cancellationToken);

                decimal percent = goal.TargetAmount > 0 ? Math.Round((achievedAmount / goal.TargetAmount) * 100m, 2) : 0m;

                result.Add(new CommercialGoalProgressModel
                {
                    Id = goal.Id,
                    UserId = goal.UserId,
                    UserName = meta.UserName,
                    PeriodType = (int)goal.PeriodType,
                    PeriodStart = goal.PeriodStart,
                    PeriodEnd = periodEnd,
                    TargetAmount = goal.TargetAmount,
                    AchievedAmount = achievedAmount,
                    AchievedDealsCount = achievedCount,
                    PercentAchieved = percent
                });
            }

            return result;
        }

        private async Task<CommercialGoalModel[]> EnrichWithUserNames(IReadOnlyCollection<CommercialGoal> goals, CancellationToken cancellationToken)
        {
            if (goals.Count == 0)
            {
                return [];
            }

            Dictionary<long, string?> userNames = new();
            foreach (long userId in goals.Where(goal => goal.UserId.HasValue).Select(goal => goal.UserId!.Value).Distinct())
            {
                try
                {
                    IdentityUserDto? user = await identityUsersClient.GetUserByIdAsync(userId, cancellationToken);
                    userNames[userId] = user?.Name;
                }
                catch
                {
                    userNames[userId] = null;
                }
            }

            return goals.Select(goal => new CommercialGoalModel
            {
                Id = goal.Id,
                UserId = goal.UserId,
                UserName = goal.UserId.HasValue && userNames.TryGetValue(goal.UserId.Value, out string? name) ? name : null,
                PeriodType = (int)goal.PeriodType,
                PeriodStart = goal.PeriodStart,
                PeriodEnd = goal.PeriodEnd(),
                TargetAmount = goal.TargetAmount,
                Notes = goal.Notes,
                IsActive = goal.IsActive
            }).ToArray();
        }
    }
}
