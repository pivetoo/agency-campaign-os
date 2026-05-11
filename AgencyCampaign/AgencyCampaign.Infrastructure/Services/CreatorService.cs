using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Creators;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Runtime.CompilerServices;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorService : CrudService<Creator>, ICreatorService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CreatorService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Creator>> GetCreators(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Creator>()
                .AsNoTracking()
                .OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Creator?> GetCreatorById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Creator>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Creator> CreateCreator(CreateCreatorRequest request, CancellationToken cancellationToken = default)
        {
            Creator creator = new(request.Name, request.StageName, request.Email, request.Phone, request.Document, request.PixKey, request.PixKeyType, request.PrimaryNiche, request.City, request.State, request.Notes, request.DefaultAgencyFeePercent);
            bool success = await Insert(cancellationToken, creator);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return creator;
        }

        public async Task<Creator> UpdateCreator(long id, UpdateCreatorRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Creator? creator = await DbContext.Set<Creator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (creator is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            creator.Update(request.Name, request.StageName, request.Email, request.Phone, request.Document, request.PixKey, request.PixKeyType, request.PrimaryNiche, request.City, request.State, request.Notes, request.DefaultAgencyFeePercent, request.IsActive);

            Creator? result = await Update(creator, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public async Task<Creator> SetCreatorPhoto(long id, string photoUrl, CancellationToken cancellationToken = default)
        {
            Creator creator = await LoadTrackedCreator(id, cancellationToken);
            creator.SetPhoto(photoUrl);

            Creator? result = await Update(creator, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public async Task<Creator> RemoveCreatorPhoto(long id, CancellationToken cancellationToken = default)
        {
            Creator creator = await LoadTrackedCreator(id, cancellationToken);
            creator.SetPhoto(null);

            Creator? result = await Update(creator, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task<Creator> LoadTrackedCreator(long id, CancellationToken cancellationToken)
        {
            Creator? creator = await DbContext.Set<Creator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (creator is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return creator;
        }

        public async Task<CreatorSummaryModel?> GetSummary(long id, CancellationToken cancellationToken = default)
        {
            Creator? creator = await DbContext.Set<Creator>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (creator is null)
            {
                return null;
            }

            List<CampaignCreator> campaignCreators = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Include(item => item.CampaignCreatorStatus)
                .Where(item => item.CreatorId == id)
                .ToListAsync(cancellationToken);

            List<long> campaignCreatorIds = campaignCreators.Select(item => item.Id).ToList();

            List<CampaignDeliverable> deliverables = await DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Platform)
                .Where(item => campaignCreatorIds.Contains(item.CampaignCreatorId))
                .ToListAsync(cancellationToken);

            int totalDeliverables = deliverables.Count;
            int publishedDeliverables = deliverables.Count(item => item.Status == DeliverableStatus.Published);
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            int overdueDeliverables = deliverables.Count(item => item.Status != DeliverableStatus.Published && item.Status != DeliverableStatus.Cancelled && item.DueAt < utcNow);

            int onTimePublished = deliverables.Count(item => item.Status == DeliverableStatus.Published && item.PublishedAt.HasValue && item.PublishedAt.Value <= item.DueAt);
            decimal onTimeRate = publishedDeliverables == 0 ? 0 : Math.Round((decimal)onTimePublished / publishedDeliverables * 100m, 2);

            var perPlatform = deliverables
                .GroupBy(item => new { item.PlatformId, PlatformName = item.Platform?.Name ?? string.Empty })
                .Select(group => new CreatorPerformanceByPlatformModel
                {
                    PlatformId = group.Key.PlatformId,
                    PlatformName = group.Key.PlatformName,
                    Deliverables = group.Count(),
                    Published = group.Count(item => item.Status == DeliverableStatus.Published),
                    GrossAmount = group.Sum(item => item.GrossAmount)
                })
                .OrderByDescending(item => item.GrossAmount)
                .ToArray();

            return new CreatorSummaryModel
            {
                CreatorId = creator.Id,
                CreatorName = creator.StageName ?? creator.Name,
                TotalCampaigns = campaignCreators.Select(item => item.CampaignId).Distinct().Count(),
                ConfirmedCampaigns = campaignCreators.Count(item => item.ConfirmedAt.HasValue),
                CancelledCampaigns = campaignCreators.Count(item => item.CancelledAt.HasValue),
                TotalDeliverables = totalDeliverables,
                PublishedDeliverables = publishedDeliverables,
                OverdueDeliverables = overdueDeliverables,
                TotalGrossAmount = deliverables.Sum(item => item.GrossAmount),
                TotalCreatorAmount = deliverables.Sum(item => item.CreatorAmount),
                TotalAgencyFeeAmount = deliverables.Sum(item => item.AgencyFeeAmount),
                OnTimeDeliveryRate = onTimeRate,
                PerformanceByPlatform = perPlatform
            };
        }

        public async Task<IReadOnlyCollection<CampaignCreator>> GetCampaignsByCreator(long creatorId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.CampaignCreatorStatus)
                .Where(item => item.CreatorId == creatorId)
                .OrderByDescending(item => item.Id)
                .ToArrayAsync(cancellationToken);
        }

        public async IAsyncEnumerable<string> ExportAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (Creator creator in DbContext.Set<Creator>()
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return CsvLine(
                    creator.Name,
                    creator.StageName,
                    creator.PrimaryNiche,
                    creator.City,
                    creator.State,
                    creator.Email,
                    creator.Phone,
                    creator.Document,
                    creator.DefaultAgencyFeePercent.ToString("F2"),
                    creator.IsActive ? "Sim" : "Não");
            }
        }

        private static string CsvLine(params string?[] fields) =>
            string.Join(",", fields.Select(EscapeField));

        private static string EscapeField(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
