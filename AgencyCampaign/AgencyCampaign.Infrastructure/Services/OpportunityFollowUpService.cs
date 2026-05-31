using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityFollowUpService : CrudService<OpportunityFollowUp>, IOpportunityFollowUpService
    {
        private readonly INotificationService notificationService;

        public OpportunityFollowUpService(DbContext dbContext, INotificationService notificationService) : base(dbContext)
        {
            this.notificationService = notificationService;
        }

        public async Task<int> RemindDue(CancellationToken cancellationToken = default)
        {
            DateTimeOffset cutoff = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(1).AddTicks(-1);

            List<OpportunityFollowUp> due = await DbContext.Set<OpportunityFollowUp>()
                .AsTracking()
                .Include(item => item.Opportunity)
                .Where(item => !item.IsCompleted
                    && item.ReminderSentAt == null
                    && item.DueAt <= cutoff
                    && item.Opportunity != null
                    && item.Opportunity.ClosedAt == null)
                .ToListAsync(cancellationToken);

            int reminded = 0;
            foreach (OpportunityFollowUp followUp in due)
            {
                try
                {
                    await notificationService.Create(KanvasNotifications.FollowUpDue(followUp, followUp.Opportunity!), cancellationToken);
                    followUp.MarkReminderSent();
                    reminded++;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"[OpportunityFollowUpService] failed to remind follow-up {followUp.Id}: {exception.Message}");
                }
            }

            if (reminded > 0)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            return reminded;
        }

        public async Task<OpportunityFollowUp?> GetOpportunityFollowUpById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<OpportunityFollowUp> CreateOpportunityFollowUp(CreateOpportunityFollowUpRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureOpportunityExists(request.OpportunityId, cancellationToken);

            OpportunityFollowUp followUp = new(
                request.OpportunityId,
                request.Subject,
                request.DueAt,
                request.Notes);

            bool success = await Insert(cancellationToken, followUp);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityFollowUpById(followUp.Id, cancellationToken) ?? followUp;
        }

        public async Task<OpportunityFollowUp> UpdateOpportunityFollowUp(long id, UpdateOpportunityFollowUpRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityFollowUp? followUp = await DbContext.Set<OpportunityFollowUp>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (followUp is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            followUp.Update(request.Subject, request.DueAt, request.Notes);

            OpportunityFollowUp? result = await Update(followUp, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityFollowUpById(id, cancellationToken) ?? followUp;
        }

        public async Task<OpportunityFollowUp> CompleteOpportunityFollowUp(long id, CancellationToken cancellationToken = default)
        {
            OpportunityFollowUp? followUp = await DbContext.Set<OpportunityFollowUp>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (followUp is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            followUp.Complete();

            OpportunityFollowUp? result = await Update(followUp, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityFollowUpById(id, cancellationToken) ?? followUp;
        }

        public async Task DeleteOpportunityFollowUp(long id, CancellationToken cancellationToken = default)
        {
            OpportunityFollowUp? followUp = await DbContext.Set<OpportunityFollowUp>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (followUp is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            await Delete([followUp], cancellationToken);
        }

        public async Task<IReadOnlyCollection<OpportunityFollowUp>> GetFollowUpsByOpportunityId(long opportunityId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .Where(item => item.OpportunityId == opportunityId)
                .OrderBy(item => item.IsCompleted)
                .ThenBy(item => item.DueAt)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<OpportunityFollowUp>> GetAllFollowUps(string? status, CancellationToken cancellationToken = default)
        {
            var today = DateTimeOffset.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            IQueryable<OpportunityFollowUp> query = DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .Include(item => item.Opportunity)
                    .ThenInclude(o => o!.Brand);

            query = status switch
            {
                "overdue" => query.Where(item => !item.IsCompleted && item.DueAt < today),
                "today" => query.Where(item => !item.IsCompleted && item.DueAt >= today && item.DueAt < tomorrow),
                "upcoming" => query.Where(item => !item.IsCompleted && item.DueAt >= tomorrow),
                "completed" => query.Where(item => item.IsCompleted),
                _ => query,
            };

            return await query
                .OrderBy(item => item.IsCompleted)
                .ThenBy(item => item.DueAt)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<FollowUpSummaryModel> GetFollowUpsSummary(CancellationToken cancellationToken = default)
        {
            var today = DateTimeOffset.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var all = await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .Select(item => new { item.IsCompleted, item.DueAt })
                .ToListAsync(cancellationToken);

            return new FollowUpSummaryModel
            {
                Overdue = all.Count(item => !item.IsCompleted && item.DueAt < today),
                Today = all.Count(item => !item.IsCompleted && item.DueAt >= today && item.DueAt < tomorrow),
                Upcoming = all.Count(item => !item.IsCompleted && item.DueAt >= tomorrow),
                Completed = all.Count(item => item.IsCompleted),
            };
        }

        private async Task EnsureOpportunityExists(long opportunityId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == opportunityId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }
    }
}
