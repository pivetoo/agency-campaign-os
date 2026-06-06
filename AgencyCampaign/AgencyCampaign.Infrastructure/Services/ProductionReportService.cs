using AgencyCampaign.Application.Models.Production;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProductionReportService : IProductionReportService
    {
        private readonly DbContext dbContext;

        public ProductionReportService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<CampaignPerformanceModel> GetCampaignPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<CampaignDeliverable> deliverables = await LoadPublished(normalizedFrom, normalizedTo, cancellationToken);

            decimal? emvRate = await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .Select(item => item.EmvCpmRate)
                .FirstOrDefaultAsync(cancellationToken);

            CampaignPerformanceLineModel[] lines = deliverables
                .GroupBy(d => d.CampaignId)
                .Select(group =>
                {
                    long totalReach = group.Sum(d => d.Reach ?? 0);
                    long totalImpressions = group.Sum(d => d.Impressions ?? 0);
                    long totalEngagement = group.Sum(Engagement);

                    List<decimal> rates = group
                        .Where(d => d.EngagementRate.HasValue)
                        .Select(d => d.EngagementRate!.Value)
                        .ToList();
                    decimal? avgEngagementRate = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null;

                    long emvBase = totalImpressions > 0 ? totalImpressions : totalReach;
                    decimal? emv = emvRate.HasValue && emvRate.Value > 0 && emvBase > 0
                        ? Math.Round(emvBase / 1000m * emvRate.Value, 2)
                        : null;

                    CampaignDeliverable first = group.First();
                    return new CampaignPerformanceLineModel
                    {
                        CampaignId = group.Key,
                        CampaignName = first.Campaign?.Name ?? string.Empty,
                        BrandName = first.Campaign?.Brand?.Name,
                        Deliverables = group.Count(),
                        TotalReach = totalReach,
                        TotalImpressions = totalImpressions,
                        TotalEngagement = totalEngagement,
                        AvgEngagementRate = avgEngagementRate,
                        Emv = emv
                    };
                })
                .OrderByDescending(l => l.TotalReach)
                .Take(50)
                .ToArray();

            return new CampaignPerformanceModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines
            };
        }

        public async Task<CreatorPerformanceModel> GetCreatorPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<CampaignDeliverable> deliverables = await LoadPublished(normalizedFrom, normalizedTo, cancellationToken);

            CreatorPerformanceLineModel[] lines = deliverables
                .GroupBy(d => d.CampaignCreator!.CreatorId)
                .Select(group =>
                {
                    long totalReach = group.Sum(d => d.Reach ?? 0);
                    long totalEngagement = group.Sum(Engagement);

                    List<decimal> rates = group
                        .Where(d => d.EngagementRate.HasValue)
                        .Select(d => d.EngagementRate!.Value)
                        .ToList();
                    decimal? avgEngagementRate = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null;

                    CampaignDeliverable first = group.First();
                    string creatorName = first.CampaignCreator?.Creator?.StageName
                        ?? first.CampaignCreator?.Creator?.Name
                        ?? string.Empty;

                    return new CreatorPerformanceLineModel
                    {
                        CreatorId = group.Key,
                        CreatorName = creatorName,
                        Campaigns = group.Select(d => d.CampaignId).Distinct().Count(),
                        Deliverables = group.Count(),
                        TotalReach = totalReach,
                        TotalEngagement = totalEngagement,
                        AvgEngagementRate = avgEngagementRate
                    };
                })
                .OrderByDescending(l => l.TotalReach)
                .Take(50)
                .ToArray();

            return new CreatorPerformanceModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines
            };
        }

        public async Task<PlatformProductionModel> GetPlatformProduction(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<CampaignDeliverable> deliverables = await LoadPublished(normalizedFrom, normalizedTo, cancellationToken);

            PlatformProductionLineModel[] lines = deliverables
                .GroupBy(d => d.PlatformId)
                .Select(group =>
                {
                    long totalReach = group.Sum(d => d.Reach ?? 0);
                    long totalImpressions = group.Sum(d => d.Impressions ?? 0);
                    long totalEngagement = group.Sum(Engagement);

                    List<decimal> rates = group
                        .Where(d => d.EngagementRate.HasValue)
                        .Select(d => d.EngagementRate!.Value)
                        .ToList();
                    decimal? avgEngagementRate = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null;

                    CampaignDeliverable first = group.First();
                    return new PlatformProductionLineModel
                    {
                        PlatformId = group.Key,
                        PlatformName = first.Platform?.Name ?? string.Empty,
                        Deliverables = group.Count(),
                        TotalReach = totalReach,
                        TotalImpressions = totalImpressions,
                        TotalEngagement = totalEngagement,
                        AvgEngagementRate = avgEngagementRate
                    };
                })
                .OrderByDescending(l => l.TotalReach)
                .ToArray();

            return new PlatformProductionModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines
            };
        }

        public async Task<DeliverableSlaModel> GetDeliverableSla(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<CampaignDeliverable> deliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(d => d.Campaign)
                .Where(d => d.DueAt >= normalizedFrom && d.DueAt < normalizedTo && d.Status != DeliverableStatus.Cancelled)
                .ToListAsync(cancellationToken);

            int publishedOnTime = 0;
            int publishedLate = 0;
            int overdue = 0;
            int upcoming = 0;

            foreach (CampaignDeliverable d in deliverables)
            {
                if (d.Status == DeliverableStatus.Published)
                {
                    DateTimeOffset publishedAt = d.PublishedAt ?? d.DueAt.AddDays(1);
                    if (publishedAt <= d.DueAt)
                    {
                        publishedOnTime++;
                    }
                    else
                    {
                        publishedLate++;
                    }
                }
                else
                {
                    if (d.DueAt < now)
                    {
                        overdue++;
                    }
                    else
                    {
                        upcoming++;
                    }
                }
            }

            int publishedTotal = publishedOnTime + publishedLate;
            decimal onTimeRate = publishedTotal > 0
                ? Math.Round((decimal)publishedOnTime / publishedTotal * 100, 2)
                : 0;

            DeliverableSlaCampaignLineModel[] byCampaign = deliverables
                .GroupBy(d => d.CampaignId)
                .Select(group =>
                {
                    int grpOnTime = 0;
                    int grpLate = 0;
                    int grpOverdue = 0;
                    int grpUpcoming = 0;

                    foreach (CampaignDeliverable d in group)
                    {
                        if (d.Status == DeliverableStatus.Published)
                        {
                            DateTimeOffset publishedAt = d.PublishedAt ?? d.DueAt.AddDays(1);
                            if (publishedAt <= d.DueAt)
                            {
                                grpOnTime++;
                            }
                            else
                            {
                                grpLate++;
                            }
                        }
                        else
                        {
                            if (d.DueAt < now)
                            {
                                grpOverdue++;
                            }
                            else
                            {
                                grpUpcoming++;
                            }
                        }
                    }

                    CampaignDeliverable first = group.First();
                    return new DeliverableSlaCampaignLineModel
                    {
                        CampaignId = group.Key,
                        CampaignName = first.Campaign?.Name ?? string.Empty,
                        Total = group.Count(),
                        PublishedOnTime = grpOnTime,
                        PublishedLate = grpLate,
                        Overdue = grpOverdue,
                        Upcoming = grpUpcoming
                    };
                })
                .OrderByDescending(line => line.Total)
                .ToArray();

            return new DeliverableSlaModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                PublishedOnTime = publishedOnTime,
                PublishedLate = publishedLate,
                Overdue = overdue,
                Upcoming = upcoming,
                OnTimeRate = onTimeRate,
                ByCampaign = byCampaign
            };
        }

        public async Task<ApprovalCycleModel> GetApprovalCycle(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<DeliverableApproval> approvals = await dbContext.Set<DeliverableApproval>()
                .AsNoTracking()
                .Where(a => a.Status == DeliverableApprovalStatus.Approved
                    && a.ApprovedAt != null
                    && a.ApprovedAt >= normalizedFrom
                    && a.ApprovedAt < normalizedTo)
                .ToListAsync(cancellationToken);

            List<DeliverableApproval> internalApprovals = approvals
                .Where(a => a.ApprovalType == DeliverableApprovalType.Internal)
                .ToList();

            List<DeliverableApproval> brandApprovals = approvals
                .Where(a => a.ApprovalType == DeliverableApprovalType.Brand)
                .ToList();

            decimal? avgInternalDays = internalApprovals.Count > 0
                ? Math.Round((decimal)internalApprovals.Average(a => (a.ApprovedAt!.Value - a.CreatedAt).TotalDays), 2)
                : null;

            decimal? avgBrandDays = brandApprovals.Count > 0
                ? Math.Round((decimal)brandApprovals.Average(a => (a.ApprovedAt!.Value - a.CreatedAt).TotalDays), 2)
                : null;

            // Janela ancorada em CreatedAt da versao: DeliverableContentVersion nao tem ApprovedAt; a versao
            // aprovada (RoundNumber) e a fonte de "rodadas ate aprovar".
            List<DeliverableContentVersion> versions = await dbContext.Set<DeliverableContentVersion>()
                .AsNoTracking()
                .Where(v => v.Status == ContentVersionStatus.Approved
                    && v.CreatedAt >= normalizedFrom
                    && v.CreatedAt < normalizedTo)
                .ToListAsync(cancellationToken);

            int contentApprovedCount = versions.Count;
            decimal? avgRounds = contentApprovedCount > 0
                ? Math.Round((decimal)versions.Average(v => v.RoundNumber), 2)
                : null;

            decimal? firstRoundApprovalRate = contentApprovedCount > 0
                ? Math.Round((decimal)versions.Count(v => v.RoundNumber == 1) / contentApprovedCount * 100, 2)
                : null;

            return new ApprovalCycleModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                InternalApprovedCount = internalApprovals.Count,
                BrandApprovedCount = brandApprovals.Count,
                AvgInternalApprovalDays = avgInternalDays,
                AvgBrandApprovalDays = avgBrandDays,
                ContentApprovedCount = contentApprovedCount,
                AvgRounds = avgRounds,
                FirstRoundApprovalRate = firstRoundApprovalRate
            };
        }

        public async Task<ContentLicenseReportModel> GetContentLicenses(int expiringSoonDays, CancellationToken cancellationToken = default)
        {
            int threshold = expiringSoonDays <= 0 ? 30 : expiringSoonDays;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<DeliverableContentLicense> licenses = await dbContext.Set<DeliverableContentLicense>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<long> deliverableIds = licenses.Select(l => l.CampaignDeliverableId).Distinct().ToList();

            List<CampaignDeliverable> deliverables = await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(d => d.Campaign)
                .Where(d => deliverableIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            Dictionary<long, CampaignDeliverable> deliverableMap = deliverables.ToDictionary(d => d.Id);

            int activeCount = 0;
            int expiringSoonCount = 0;
            int expiredCount = 0;

            List<ContentLicenseReportLineModel> lines = new();

            foreach (DeliverableContentLicense license in licenses)
            {
                ContentLicenseStatus status = license.ComputeStatus(now, threshold);
                int? days = license.DaysUntilExpiry(now);

                deliverableMap.TryGetValue(license.CampaignDeliverableId, out CampaignDeliverable? deliverable);

                if (status == ContentLicenseStatus.Active)
                {
                    activeCount++;
                }
                else if (status == ContentLicenseStatus.ExpiringSoon)
                {
                    expiringSoonCount++;
                }
                else
                {
                    expiredCount++;
                }

                lines.Add(new ContentLicenseReportLineModel
                {
                    LicenseId = license.Id,
                    CampaignDeliverableId = license.CampaignDeliverableId,
                    DeliverableTitle = deliverable?.Title ?? string.Empty,
                    CampaignName = deliverable?.Campaign?.Name,
                    Type = (int)license.Type,
                    Channels = license.Channels,
                    StartsAt = license.StartsAt,
                    ExpiresAt = license.ExpiresAt,
                    DaysUntilExpiry = days,
                    Status = (int)status
                });
            }

            ContentLicenseReportLineModel[] orderedLines = lines
                .OrderBy(l => l.ExpiresAt == null)
                .ThenBy(l => l.ExpiresAt)
                .ToArray();

            return new ContentLicenseReportModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                ExpiringSoonDays = threshold,
                ActiveCount = activeCount,
                ExpiringSoonCount = expiringSoonCount,
                ExpiredCount = expiredCount,
                Lines = orderedLines
            };
        }

        private async Task<List<CampaignDeliverable>> LoadPublished(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        {
            return await dbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(d => d.Campaign).ThenInclude(c => c!.Brand)
                .Include(d => d.Platform)
                .Include(d => d.CampaignCreator).ThenInclude(cc => cc!.Creator)
                .Where(d => d.PublishedAt != null && d.PublishedAt >= from && d.PublishedAt < to)
                .ToListAsync(ct);
        }

        private static long Engagement(CampaignDeliverable d)
        {
            return (d.Likes ?? 0) + (d.Comments ?? 0) + (d.Shares ?? 0) + (d.Saves ?? 0);
        }
    }
}
