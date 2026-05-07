using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Creators;
using AgencyCampaign.Application.Requests.CreatorSocialHandles;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorSocialHandleService : ICreatorSocialHandleService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CreatorSocialHandleService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<CreatorSocialHandleModel>> GetByCreator(long creatorId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<CreatorSocialHandle>()
                .AsNoTracking()
                .Include(item => item.Platform)
                .Where(item => item.CreatorId == creatorId)
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => item.Platform!.Name)
                .Select(item => Map(item))
                .ToArrayAsync(cancellationToken);
        }

        public async Task<CreatorSocialHandleModel> Create(CreateCreatorSocialHandleRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CreatorId, request.PlatformId, cancellationToken);

            CreatorSocialHandle handle = new(
                request.CreatorId,
                request.PlatformId,
                request.Handle,
                request.ProfileUrl,
                request.Followers,
                request.EngagementRate,
                request.IsPrimary);

            dbContext.Set<CreatorSocialHandle>().Add(handle);
            await dbContext.SaveChangesAsync(cancellationToken);

            return await ReloadWithPlatform(handle.Id, cancellationToken);
        }

        public async Task<CreatorSocialHandleModel> Update(long id, UpdateCreatorSocialHandleRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CreatorSocialHandle? handle = await dbContext.Set<CreatorSocialHandle>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (handle is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            handle.Update(
                request.PlatformId,
                request.Handle,
                request.ProfileUrl,
                request.Followers,
                request.EngagementRate,
                request.IsPrimary,
                request.IsActive);

            await dbContext.SaveChangesAsync(cancellationToken);

            return await ReloadWithPlatform(handle.Id, cancellationToken);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            CreatorSocialHandle? handle = await dbContext.Set<CreatorSocialHandle>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (handle is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            dbContext.Set<CreatorSocialHandle>().Remove(handle);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<CreatorSocialHandleModel> ReloadWithPlatform(long id, CancellationToken cancellationToken)
        {
            CreatorSocialHandle? handle = await dbContext.Set<CreatorSocialHandle>()
                .AsNoTracking()
                .Include(item => item.Platform)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (handle is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return Map(handle);
        }

        private async Task EnsureReferencesExist(long creatorId, long platformId, CancellationToken cancellationToken)
        {
            bool creatorExists = await dbContext.Set<Creator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == creatorId, cancellationToken);

            if (!creatorExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            bool platformExists = await dbContext.Set<Platform>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == platformId && item.IsActive, cancellationToken);

            if (!platformExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private static CreatorSocialHandleModel Map(CreatorSocialHandle handle) => new()
        {
            Id = handle.Id,
            CreatorId = handle.CreatorId,
            PlatformId = handle.PlatformId,
            PlatformName = handle.Platform?.Name ?? string.Empty,
            Handle = handle.Handle,
            ProfileUrl = handle.ProfileUrl,
            Followers = handle.Followers,
            EngagementRate = handle.EngagementRate,
            IsPrimary = handle.IsPrimary,
            IsActive = handle.IsActive
        };
    }
}
