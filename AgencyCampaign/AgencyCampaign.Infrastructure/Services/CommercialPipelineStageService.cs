using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CommercialPipelineStages;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CommercialPipelineStageService : CrudService<CommercialPipelineStage>, ICommercialPipelineStageService
    {

        public CommercialPipelineStageService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<CommercialPipelineStage>> GetStages(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<CommercialPipelineStage> query = DbContext.Set<CommercialPipelineStage>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower));
            }

            return await query
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
            // Novo estagio nasce ativo; barra um segundo Ganha/Perdida ativo (fechamento nao-deterministico)
            await EnsureNoDuplicateActiveFinalStage(request.IsFinal, request.FinalBehavior, isActive: true, currentId: null, cancellationToken);

            CommercialPipelineStage stage = new(request.Name, request.DisplayOrder, request.Color, request.Description, request.IsInitial, request.IsFinal, request.FinalBehavior, request.DefaultProbability, request.SlaInDays);
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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            CommercialPipelineStage? stage = await DbContext.Set<CommercialPipelineStage>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (stage is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            await EnsureInitialStageRules(request.IsInitial, stage.Id, cancellationToken);
            // Avalia o estado ATUAL do estagio (antes do Update) para nao remover o ultimo Ganha/Perdida ativo
            await EnsureFinalStageRulesOnUpdate(stage, request, cancellationToken);
            stage.Update(request.Name, request.DisplayOrder, request.Color, request.Description, request.IsInitial, request.IsFinal, request.FinalBehavior, request.IsActive, request.DefaultProbability, request.SlaInDays);

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
                throw new InvalidOperationException("commercialPipelineStage.initial.duplicate");
            }
        }

        private async Task EnsureNoDuplicateActiveFinalStage(bool isFinal, CommercialPipelineStageFinalBehavior finalBehavior, bool isActive, long? currentId, CancellationToken cancellationToken)
        {
            if (!isActive || !isFinal || finalBehavior == CommercialPipelineStageFinalBehavior.None)
            {
                return;
            }

            bool anotherActiveSameBehavior = await DbContext.Set<CommercialPipelineStage>()
                .AsNoTracking()
                .AnyAsync(item => item.IsActive && item.IsFinal && item.FinalBehavior == finalBehavior && item.Id != currentId, cancellationToken);

            if (anotherActiveSameBehavior)
            {
                throw new InvalidOperationException("commercialPipelineStage.final.duplicate");
            }
        }

        private async Task EnsureFinalStageRulesOnUpdate(CommercialPipelineStage current, UpdateCommercialPipelineStageRequest request, CancellationToken cancellationToken)
        {
            CommercialPipelineStageFinalBehavior resultingBehavior = request.IsFinal ? request.FinalBehavior : CommercialPipelineStageFinalBehavior.None;

            await EnsureNoDuplicateActiveFinalStage(request.IsFinal, resultingBehavior, request.IsActive, current.Id, cancellationToken);

            foreach (CommercialPipelineStageFinalBehavior behavior in new[] { CommercialPipelineStageFinalBehavior.Won, CommercialPipelineStageFinalBehavior.Lost })
            {
                bool wasThisBehavior = current.IsActive && current.IsFinal && current.FinalBehavior == behavior;
                bool willBeThisBehavior = request.IsActive && resultingBehavior == behavior;
                if (!wasThisBehavior || willBeThisBehavior)
                {
                    continue;
                }

                bool anotherActiveBehavior = await DbContext.Set<CommercialPipelineStage>()
                    .AsNoTracking()
                    .AnyAsync(item => item.IsActive && item.IsFinal && item.FinalBehavior == behavior && item.Id != current.Id, cancellationToken);

                if (!anotherActiveBehavior)
                {
                    throw new InvalidOperationException("commercialPipelineStage.final.required");
                }
            }
        }
    }
}
