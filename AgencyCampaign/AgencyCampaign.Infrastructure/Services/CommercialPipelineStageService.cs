using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CommercialPipelineStages;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CommercialPipelineStageService : CrudService<CommercialPipelineStage>, ICommercialPipelineStageService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CommercialPipelineStageService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<CommercialPipelineStage>> GetStages(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<List<CommercialPipelineStage>> GetActiveStages(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<CommercialPipelineStage?> GetStageById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<CommercialPipelineStage> CreateStage(CreateCommercialPipelineStageRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureInitialStageRules(request.IsInitial, null, cancellationToken);

            CommercialPipelineStage stage = new(request.Name, request.DisplayOrder, request.Color, request.Description, request.IsInitial, request.IsFinal, request.FinalBehavior);
            bool success = await Insert(cancellationToken, stage);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return stage;
        }

        public async Task<CommercialPipelineStage> UpdateStage(long id, UpdateCommercialPipelineStageRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CommercialPipelineStage? stage = await DbContext.Set<CommercialPipelineStage>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (stage is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureInitialStageRules(request.IsInitial, stage.Id, cancellationToken);
            stage.Update(request.Name, request.DisplayOrder, request.Color, request.Description, request.IsInitial, request.IsFinal, request.FinalBehavior, request.IsActive);

            CommercialPipelineStage? result = await Update(stage, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsureInitialStageRules(bool isInitial, long? currentId, CancellationToken cancellationToken)
        {
            if (!isInitial)
            {
                return;
            }

            bool anotherInitialExists = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .AnyAsync(item => item.IsInitial && item.Id != currentId, cancellationToken);

            if (anotherInitialExists)
            {
                throw new InvalidOperationException("Já existe um estágio inicial configurado para o pipeline comercial.");
            }
        }
    }
}
