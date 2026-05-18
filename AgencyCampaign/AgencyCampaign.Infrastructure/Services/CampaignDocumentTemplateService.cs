using System.Text.RegularExpressions;
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
        private readonly ICurrentUser currentUser;

        public CampaignDocumentTemplateService(DbContext dbContext, ICurrentUser currentUser) : base(dbContext)
        {
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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            CampaignDocumentTemplate? template = await DbContext.Set<CampaignDocumentTemplate>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException("record.notFound");
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
                throw new InvalidOperationException("record.notFound");
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

        public Task<string> PreviewTemplate(string body, CampaignDocumentType documentType, CancellationToken cancellationToken = default)
        {
            string rendered = Render(body ?? string.Empty, SampleValues);
            return Task.FromResult(rendered);
        }

        private static readonly Regex PlaceholderRegex = new(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

        private static readonly IReadOnlyDictionary<string, string> SampleValues = new Dictionary<string, string>
        {
            ["today"] = DateTime.Now.ToString("dd/MM/yyyy"),
            ["campaignId"] = "1234",
            ["campaignName"] = "Lançamento Verão 2026",
            ["campaignDescription"] = "Campanha de ativação focada em conteúdo orgânico e mídia paga.",
            ["campaignObjective"] = "Aumentar reconhecimento de marca e gerar vendas diretas.",
            ["campaignBriefing"] = "Tom descontraído, foco em momentos do dia a dia com o produto.",
            ["campaignStartDate"] = "01/06/2026",
            ["campaignEndDate"] = "30/06/2026",
            ["campaignBudget"] = "R$ 45.000,00",
            ["brandName"] = "Marca Exemplo Ltda.",
            ["brandTradeName"] = "Marca Exemplo",
            ["brandDocument"] = "12.345.678/0001-90",
            ["brandContactName"] = "Joana Souza",
            ["brandContactEmail"] = "joana@marcaexemplo.com.br",
            ["creatorName"] = "Maria da Silva",
            ["creatorStageName"] = "@mariasilva",
            ["creatorEmail"] = "maria@example.com",
            ["creatorDocument"] = "123.456.789-00",
            ["creatorAgreedAmount"] = "R$ 5.000,00",
            ["creatorAgencyFeePercent"] = "20%",
            ["creatorAgencyFeeAmount"] = "R$ 1.000,00",
            ["scopeNotes"] = "Inclui 1 Reels e 3 Stories sequenciais.",
        };

        private static string Render(string template, IReadOnlyDictionary<string, string> values)
        {
            return PlaceholderRegex.Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                return values.TryGetValue(key, out string? value) ? value : string.Empty;
            });
        }
    }
}
