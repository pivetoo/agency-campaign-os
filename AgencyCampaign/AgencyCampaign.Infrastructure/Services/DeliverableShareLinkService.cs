using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Deliverables;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.DeliverableShareLinks;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Cryptography;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class DeliverableShareLinkService : IDeliverableShareLinkService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public DeliverableShareLinkService(DbContext dbContext, ICurrentUser currentUser, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<DeliverableShareLinkModel>> GetByDeliverable(long deliverableId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return await dbContext.Set<DeliverableShareLink>()
                .AsNoTracking()
                .Where(item => item.CampaignDeliverableId == deliverableId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new DeliverableShareLinkModel
                {
                    Id = item.Id,
                    CampaignDeliverableId = item.CampaignDeliverableId,
                    Token = item.Token,
                    ReviewerName = item.ReviewerName,
                    ExpiresAt = item.ExpiresAt,
                    RevokedAt = item.RevokedAt,
                    LastViewedAt = item.LastViewedAt,
                    ViewCount = item.ViewCount,
                    IsActive = !item.RevokedAt.HasValue && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > now),
                    CreatedAt = item.CreatedAt
                })
                .ToArrayAsync(cancellationToken);
        }

        public async Task<DeliverableShareLinkModel> Create(CreateDeliverableShareLinkRequest request, CancellationToken cancellationToken = default)
        {
            bool deliverableExists = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == request.CampaignDeliverableId, cancellationToken);

            if (!deliverableExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            string token = GenerateToken();

            DeliverableShareLink shareLink = new(
                request.CampaignDeliverableId,
                token,
                request.ReviewerName,
                request.ExpiresAt,
                currentUser.UserId,
                currentUser.UserName);

            dbContext.Set<DeliverableShareLink>().Add(shareLink);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new DeliverableShareLinkModel
            {
                Id = shareLink.Id,
                CampaignDeliverableId = shareLink.CampaignDeliverableId,
                Token = shareLink.Token,
                ReviewerName = shareLink.ReviewerName,
                ExpiresAt = shareLink.ExpiresAt,
                RevokedAt = shareLink.RevokedAt,
                LastViewedAt = shareLink.LastViewedAt,
                ViewCount = shareLink.ViewCount,
                IsActive = shareLink.IsActive(DateTimeOffset.UtcNow),
                CreatedAt = shareLink.CreatedAt
            };
        }

        public async Task Revoke(long id, CancellationToken cancellationToken = default)
        {
            DeliverableShareLink? shareLink = await dbContext.Set<DeliverableShareLink>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (shareLink is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            shareLink.Revoke();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string GenerateToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }

    public sealed class DeliverablePublicService : IDeliverablePublicService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly INotificationService notificationService;

        public DeliverablePublicService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, INotificationService notificationService)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
            this.notificationService = notificationService;
        }

        public async Task<DeliverablePublicViewModel?> GetByToken(string token, CancellationToken cancellationToken = default)
        {
            DeliverableShareLink? shareLink = await ResolveActiveShareLink(token, cancellationToken);
            if (shareLink is null)
            {
                return null;
            }

            shareLink.RegisterView();
            await dbContext.SaveChangesAsync(cancellationToken);

            return await BuildViewModel(shareLink.CampaignDeliverableId, cancellationToken);
        }

        public async Task<DeliverablePublicViewModel> Approve(string token, PublicDeliverableDecisionRequest request, CancellationToken cancellationToken = default)
        {
            return await SaveDecision(token, request, true, cancellationToken);
        }

        public async Task<DeliverablePublicViewModel> Reject(string token, PublicDeliverableDecisionRequest request, CancellationToken cancellationToken = default)
        {
            return await SaveDecision(token, request, false, cancellationToken);
        }

        private async Task<DeliverablePublicViewModel> SaveDecision(string token, PublicDeliverableDecisionRequest request, bool approved, CancellationToken cancellationToken)
        {
            DeliverableShareLink? shareLink = await ResolveActiveShareLink(token, cancellationToken);
            if (shareLink is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            DeliverableApproval? approval = await dbContext.Set<DeliverableApproval>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.CampaignDeliverableId == shareLink.CampaignDeliverableId && item.ApprovalType == DeliverableApprovalType.Brand, cancellationToken);

            if (approval is null)
            {
                approval = new DeliverableApproval(shareLink.CampaignDeliverableId, DeliverableApprovalType.Brand, request.ReviewerName, request.Comment);
                dbContext.Set<DeliverableApproval>().Add(approval);
            }
            else
            {
                approval.UpdateReviewer(request.ReviewerName);
            }

            if (approved)
            {
                approval.Approve(request.Comment);
            }
            else
            {
                approval.Reject(request.Comment);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            CampaignDeliverable? deliverable = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == shareLink.CampaignDeliverableId, cancellationToken);

            if (deliverable is not null)
            {
                try
                {
                    var notification = approved
                        ? KanvasNotifications.DeliverableApprovedByBrand(deliverable, request.ReviewerName)
                        : KanvasNotifications.DeliverableRejectedByBrand(deliverable, request.ReviewerName, request.Comment);
                    await notificationService.Create(notification, cancellationToken);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"[DeliverablePublicService] failed to create notification: {exception.Message}");
                }
            }

            return await BuildViewModel(shareLink.CampaignDeliverableId, cancellationToken)
                ?? throw new InvalidOperationException(localizer["record.notFound"]);
        }

        private async Task<DeliverableShareLink?> ResolveActiveShareLink(string token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            DeliverableShareLink? shareLink = await dbContext.Set<DeliverableShareLink>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (shareLink is null || !shareLink.IsActive(DateTimeOffset.UtcNow))
            {
                return null;
            }

            return shareLink;
        }

        private async Task<DeliverablePublicViewModel?> BuildViewModel(long deliverableId, CancellationToken cancellationToken)
        {
            CampaignDeliverable? deliverable = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.CampaignCreator!)
                    .ThenInclude(item => item.Creator)
                .Include(item => item.DeliverableKind)
                .Include(item => item.Platform)
                .Include(item => item.Approvals)
                .FirstOrDefaultAsync(item => item.Id == deliverableId, cancellationToken);

            if (deliverable is null)
            {
                return null;
            }

            DeliverableApproval? brandApproval = deliverable.Approvals.FirstOrDefault(item => item.ApprovalType == DeliverableApprovalType.Brand);

            return new DeliverablePublicViewModel
            {
                DeliverableId = deliverable.Id,
                Title = deliverable.Title,
                Description = deliverable.Description,
                CreatorName = deliverable.CampaignCreator?.Creator?.StageName ?? deliverable.CampaignCreator?.Creator?.Name,
                PlatformName = deliverable.Platform?.Name,
                DeliverableKindName = deliverable.DeliverableKind?.Name,
                CampaignName = deliverable.Campaign?.Name,
                BrandName = deliverable.Campaign?.Brand?.Name,
                DueAt = deliverable.DueAt,
                PublishedUrl = deliverable.PublishedUrl,
                EvidenceUrl = deliverable.EvidenceUrl,
                Status = (int)deliverable.Status,
                ApprovalStatus = brandApproval is null ? null : (int)brandApproval.Status,
                ApprovalComment = brandApproval?.Comment
            };
        }
    }

    public sealed class DeliverableApprovalsService : IDeliverableApprovalsService
    {
        private readonly DbContext dbContext;

        public DeliverableApprovalsService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<PendingApprovalModel>> GetPending(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<CampaignDeliverable> deliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.CampaignCreator!)
                    .ThenInclude(item => item.Creator)
                .Include(item => item.Platform)
                .Include(item => item.Approvals)
                .Where(item =>
                    item.Status != DeliverableStatus.Published &&
                    item.Status != DeliverableStatus.Cancelled)
                .ToListAsync(cancellationToken);

            List<long> deliverableIds = deliverables.Select(item => item.Id).ToList();

            HashSet<long> withActiveShareLink = (await dbContext.Set<DeliverableShareLink>()
                .AsNoTracking()
                .Where(item =>
                    deliverableIds.Contains(item.CampaignDeliverableId) &&
                    !item.RevokedAt.HasValue &&
                    (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > now))
                .Select(item => item.CampaignDeliverableId)
                .ToArrayAsync(cancellationToken)).ToHashSet();

            return deliverables
                .Where(item => item.Approvals.All(approval => approval.Status != DeliverableApprovalStatus.Approved || approval.ApprovalType != DeliverableApprovalType.Brand))
                .OrderBy(item => item.DueAt)
                .Select(item => new PendingApprovalModel
                {
                    DeliverableId = item.Id,
                    DeliverableTitle = item.Title,
                    CampaignName = item.Campaign?.Name,
                    BrandName = item.Campaign?.Brand?.Name,
                    CreatorName = item.CampaignCreator?.Creator?.StageName ?? item.CampaignCreator?.Creator?.Name,
                    PlatformName = item.Platform?.Name,
                    DueAt = item.DueAt,
                    DeliverableStatus = (int)item.Status,
                    HasActiveShareLink = withActiveShareLink.Contains(item.Id),
                    Approvals = item.Approvals.Select(approval => new DeliverableApprovalModel
                    {
                        Id = approval.Id,
                        CampaignDeliverableId = approval.CampaignDeliverableId,
                        ApprovalType = (int)approval.ApprovalType,
                        Status = (int)approval.Status,
                        ReviewerName = approval.ReviewerName,
                        Comment = approval.Comment,
                        ApprovedAt = approval.ApprovedAt,
                        RejectedAt = approval.RejectedAt
                    }).ToArray()
                })
                .ToArray();
        }
    }
}
