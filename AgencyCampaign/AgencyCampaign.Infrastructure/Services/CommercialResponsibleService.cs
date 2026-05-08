using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.CommercialResponsibles;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.IdentityManagement;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CommercialResponsibleService : CrudService<CommercialResponsible>, ICommercialResponsibleService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly IdentityUsersClient identityUsersClient;

        public CommercialResponsibleService(
            DbContext dbContext,
            IStringLocalizer<AgencyCampaignResource> localizer,
            IdentityUsersClient identityUsersClient) : base(dbContext)
        {
            this.localizer = localizer;
            this.identityUsersClient = identityUsersClient;
        }

        public async Task<PagedResult<CommercialResponsible>> GetCommercialResponsibles(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CommercialResponsible>()
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CommercialResponsible?> GetCommercialResponsibleById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CommercialResponsible>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<CommercialResponsible> CreateCommercialResponsible(CreateCommercialResponsibleRequest request, CancellationToken cancellationToken = default)
        {
            bool alreadyExists = await DbContext.Set<CommercialResponsible>()
                .AsNoTracking()
                .AnyAsync(item => item.UserId == request.UserId, cancellationToken);

            if (alreadyExists)
            {
                throw new InvalidOperationException("Este usuário já está cadastrado como responsável comercial.");
            }

            IdentityUserDto? user = await identityUsersClient.GetUserByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException("Usuário não encontrado no IdentityManagement.");
            }

            if (!user.IsActive)
            {
                throw new InvalidOperationException("Usuário do IdentityManagement está inativo.");
            }

            CommercialResponsible responsible = new(
                user.Id,
                user.Name,
                user.Email,
                phone: null,
                request.Notes);

            bool success = await Insert(cancellationToken, responsible);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return responsible;
        }

        public async Task<CommercialResponsible> UpdateCommercialResponsible(long id, UpdateCommercialResponsibleRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CommercialResponsible? responsible = await DbContext.Set<CommercialResponsible>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (responsible is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            responsible.Update(request.Notes, request.IsActive);

            CommercialResponsible? result = await Update(responsible, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public async Task<CommercialResponsible> SyncFromIdentityManagement(long id, CancellationToken cancellationToken = default)
        {
            CommercialResponsible? responsible = await DbContext.Set<CommercialResponsible>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (responsible is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            IdentityUserDto? user = await identityUsersClient.GetUserByIdAsync(responsible.UserId, cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException("Usuário não encontrado no IdentityManagement.");
            }

            responsible.RefreshFromUser(user.Name, user.Email, phone: null);
            await DbContext.SaveChangesAsync(cancellationToken);

            return responsible;
        }

        public async Task<IReadOnlyCollection<CommercialUserModel>> GetAvailableUsers(CancellationToken cancellationToken = default)
        {
            List<IdentityUserDto> all = await identityUsersClient.GetActiveUsersAsync(cancellationToken);

            HashSet<long> alreadyLinked = await DbContext.Set<CommercialResponsible>()
                .AsNoTracking()
                .Select(item => item.UserId)
                .ToHashSetAsync(cancellationToken);

            return all
                .Where(item => !alreadyLinked.Contains(item.Id))
                .Select(item => new CommercialUserModel
                {
                    Id = item.Id,
                    Username = item.Username,
                    Email = item.Email,
                    Name = item.Name,
                    AvatarUrl = item.AvatarUrl,
                    IsActive = item.IsActive,
                })
                .ToArray();
        }
    }
}
