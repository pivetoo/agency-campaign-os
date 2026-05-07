using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalPublicService : IProposalPublicService
    {
        private readonly DbContext dbContext;
        private readonly INotificationService notificationService;

        public ProposalPublicService(DbContext dbContext, INotificationService notificationService)
        {
            this.dbContext = dbContext;
            this.notificationService = notificationService;
        }

        public async Task<ProposalPublicViewModel?> GetByToken(string token, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            ProposalShareLink? shareLink = await dbContext.Set<ProposalShareLink>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (shareLink is null)
            {
                return null;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!shareLink.IsActive(now))
            {
                return null;
            }

            ProposalVersion? version = await dbContext.Set<ProposalVersion>()
                .AsNoTracking()
                .Where(item => item.ProposalId == shareLink.ProposalId)
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (version is null)
            {
                return null;
            }

            Proposal? proposal = await dbContext.Set<Proposal>()
                .AsNoTracking()
                .Include(item => item.Opportunity)
                    .ThenInclude(opp => opp!.Brand)
                .FirstOrDefaultAsync(item => item.Id == shareLink.ProposalId, cancellationToken);

            string brandName = proposal?.Opportunity?.Brand?.Name ?? string.Empty;

            shareLink.RegisterView(ipAddress, userAgent);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (shareLink.ViewCount == 1 && proposal is not null)
            {
                try
                {
                    await notificationService.Create(KanvasNotifications.ProposalViewedByBrand(proposal, brandName), cancellationToken);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"[ProposalPublicService] failed to create notification: {exception.Message}");
                }
            }

            return new ProposalPublicViewModel
            {
                ProposalId = version.ProposalId,
                VersionNumber = version.VersionNumber,
                Name = version.Name,
                Description = version.Description,
                AgencyName = string.Empty,
                BrandName = brandName,
                TotalValue = version.TotalValue,
                ValidityUntil = version.ValidityUntil,
                SentAt = version.SentAt,
                SnapshotJson = version.SnapshotJson
            };
        }
    }
}
