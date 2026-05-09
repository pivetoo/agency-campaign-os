using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocumentTemplates;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignDocumentTemplateService : CrudService<CampaignDocumentTemplate>, ICampaignDocumentTemplateService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly ICurrentUser currentUser;

        public CampaignDocumentTemplateService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, ICurrentUser currentUser) : base(dbContext)
        {
            this.localizer = localizer;
            this.currentUser = currentUser;
        }

        public async Task<PagedResult<CampaignDocumentTemplate>> GetTemplates(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignDocumentTemplate>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignDocumentTemplate?> GetTemplateById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignDocumentTemplate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignDocumentTemplate>> GetActiveByDocumentType(CampaignDocumentType documentType, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignDocumentTemplate>()
                .AsNoTracking()
                .Where(item => item.IsActive && item.DocumentType == documentType)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignDocumentTemplate> CreateTemplate(CreateCampaignDocumentTemplateRequest request, CancellationToken cancellationToken = default)
        {
            CampaignDocumentTemplate template = new(
                request.Name,
                request.DocumentType,
                request.Body,
                request.Description,
                currentUser.UserId,
                currentUser.UserName);

            bool success = await Insert(cancellationToken, template);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetTemplateById(template.Id, cancellationToken) ?? template;
        }

        public async Task<CampaignDocumentTemplate> UpdateTemplate(long id, UpdateCampaignDocumentTemplateRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CampaignDocumentTemplate? template = await DbContext.Set<CampaignDocumentTemplate>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            template.Update(request.Name, request.DocumentType, request.Body, request.Description, request.IsActive);

            CampaignDocumentTemplate? result = await Update(template, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetTemplateById(result.Id, cancellationToken) ?? result;
        }

        public async Task<bool> DeleteTemplate(long id, CancellationToken cancellationToken = default)
        {
            CampaignDocumentTemplate? template = await DbContext.Set<CampaignDocumentTemplate>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            bool inUse = await DbContext.Set<CampaignDocument>()
                .AsNoTracking()
                .AnyAsync(item => item.TemplateId == id, cancellationToken);

            if (inUse)
            {
                template.Deactivate();
                await Update(template, cancellationToken);
                return false;
            }

            CampaignDocumentTemplate? deleted = await Delete(template.Id, cancellationToken);
            return deleted is not null;
        }
    }
}
