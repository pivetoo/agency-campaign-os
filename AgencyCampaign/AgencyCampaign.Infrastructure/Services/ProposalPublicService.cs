using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalPublicService : IProposalPublicService
    {
        private readonly DbContext dbContext;
        private readonly INotificationService notificationService;
        private readonly ILogger<ProposalPublicService>? logger;

        public ProposalPublicService(DbContext dbContext, INotificationService notificationService, ILogger<ProposalPublicService>? logger = null)
        {
            this.dbContext = dbContext;
            this.notificationService = notificationService;
            this.logger = logger;
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

            if (proposal is null || !IsPubliclyAccessible(proposal, now))
            {
                return null;
            }

            string brandName = proposal.Opportunity?.Brand?.Name ?? string.Empty;

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
                    logger?.LogWarning(exception, "Failed to create proposal-viewed notification.");
                }
            }

            if (proposal.Status == ProposalStatus.Sent)
            {
                Proposal? trackedProposal = await dbContext.Set<Proposal>()
                    .AsTracking()
                    .FirstOrDefaultAsync(item => item.Id == proposal.Id, cancellationToken);

                if (trackedProposal is not null && trackedProposal.Status == ProposalStatus.Sent)
                {
                    trackedProposal.MarkAsViewed();
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            // Le o desconto congelado na versao enviada (imune a edicoes posteriores na proposta viva).
            // Versoes legadas (sem congelamento) caem no fallback para o desconto atual da proposta.
            bool hasFrozenDiscount = version.NetTotalValue.HasValue;
            decimal? discountAmount = hasFrozenDiscount ? version.DiscountAmount : proposal?.DiscountAmount;
            decimal discountValue = discountAmount.HasValue ? Math.Clamp(discountAmount.Value, 0m, version.TotalValue) : 0m;
            decimal discountPercent = version.TotalValue > 0m ? discountValue / version.TotalValue * 100m : 0m;
            decimal netTotalValue = hasFrozenDiscount ? version.NetTotalValue!.Value : version.TotalValue - discountValue;

            return new ProposalPublicViewModel
            {
                ProposalId = version.ProposalId,
                VersionNumber = version.VersionNumber,
                Name = version.Name,
                Description = version.Description,
                AgencyName = string.Empty,
                BrandName = brandName,
                BrandLogoUrl = proposal?.Opportunity?.Brand?.LogoUrl,
                TotalValue = version.TotalValue,
                DiscountAmount = discountAmount,
                DiscountPercent = discountPercent,
                DiscountValue = discountValue,
                NetTotalValue = netTotalValue,
                ValidityUntil = version.ValidityUntil,
                SentAt = version.SentAt,
                SnapshotJson = SanitizePublicSnapshot(version.SnapshotJson)
            };
        }

        private static bool IsPubliclyAccessible(Proposal proposal, DateTimeOffset now)
        {
            if (proposal.Status == ProposalStatus.Rejected
                || proposal.Status == ProposalStatus.Cancelled
                || proposal.Status == ProposalStatus.Expired)
            {
                return false;
            }

            if (proposal.ValidityUntil.HasValue && proposal.ValidityUntil.Value < now)
            {
                return false;
            }

            return true;
        }

        private static string SanitizePublicSnapshot(string snapshotJson)
        {
            if (string.IsNullOrWhiteSpace(snapshotJson))
            {
                return snapshotJson;
            }

            JsonNode? node = JsonNode.Parse(snapshotJson);
            if (node is JsonObject snapshot)
            {
                snapshot.Remove("notes");
                return snapshot.ToJsonString();
            }

            return snapshotJson;
        }
    }
}
