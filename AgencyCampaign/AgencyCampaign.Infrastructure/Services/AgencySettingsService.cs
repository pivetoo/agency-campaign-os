using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models;
using AgencyCampaign.Application.Requests.AgencySettings;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class AgencySettingsService : IAgencySettingsService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public AgencySettingsService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<AgencySettingsModel> Get(CancellationToken cancellationToken = default)
        {
            AgencySettings settings = await ResolveOrCreate(cancellationToken);
            return Map(settings);
        }

        public async Task<AgencySettingsModel> Update(UpdateAgencySettingsRequest request, CancellationToken cancellationToken = default)
        {
            AgencySettings settings = await ResolveOrCreate(cancellationToken);

            settings.Update(
                request.AgencyName,
                request.TradeName,
                request.Document,
                request.PrimaryEmail,
                request.Phone,
                request.Address,
                request.LogoUrl,
                request.PrimaryColor,
                request.DefaultEmailConnectorId,
                request.DefaultEmailPipelineId);

            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(settings);
        }

        public async Task<AgencySettingsModel> SetLogo(string logoUrl, CancellationToken cancellationToken = default)
        {
            AgencySettings settings = await ResolveOrCreate(cancellationToken);
            settings.SetLogo(logoUrl);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(settings);
        }

        public async Task<AgencySettingsModel> RemoveLogo(CancellationToken cancellationToken = default)
        {
            AgencySettings settings = await ResolveOrCreate(cancellationToken);
            settings.SetLogo(null);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(settings);
        }

        public async Task<AgencySettingsModel> SetDefaultEmailConnector(long? connectorId, CancellationToken cancellationToken = default)
        {
            AgencySettings settings = await ResolveOrCreate(cancellationToken);
            settings.Update(
                settings.AgencyName,
                settings.TradeName,
                settings.Document,
                settings.PrimaryEmail,
                settings.Phone,
                settings.Address,
                settings.LogoUrl,
                settings.PrimaryColor,
                connectorId,
                null);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(settings);
        }

        public async Task<AgencySettingsModel> SaveProposalTemplate(string? template, CancellationToken cancellationToken = default)
        {
            AgencySettings settings = await ResolveOrCreate(cancellationToken);
            settings.SetProposalHtmlTemplate(template);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(settings);
        }

        public async Task<string> PreviewProposalTemplate(string template, CancellationToken cancellationToken = default)
        {
            AgencySettings settings = await ResolveOrCreate(cancellationToken);
            return ProposalHtmlBuilder.BuildPreview(template, settings);
        }

        public Task<IReadOnlyList<ProposalLayoutModel>> GetProposalLayouts(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProposalLayoutModel> layouts = ProposalHtmlBuilder.GetLayouts();
            return Task.FromResult(layouts);
        }

        private async Task<AgencySettings> ResolveOrCreate(CancellationToken cancellationToken)
        {
            AgencySettings? existing = await dbContext.Set<AgencySettings>()
                .AsTracking()
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            AgencySettings created = new("Minha agência");
            dbContext.Set<AgencySettings>().Add(created);
            await dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }

        private static AgencySettingsModel Map(AgencySettings settings) => new()
        {
            Id = settings.Id,
            AgencyName = settings.AgencyName,
            TradeName = settings.TradeName,
            Document = settings.Document,
            PrimaryEmail = settings.PrimaryEmail,
            Phone = settings.Phone,
            Address = settings.Address,
            LogoUrl = settings.LogoUrl,
            PrimaryColor = settings.PrimaryColor,
            DefaultEmailConnectorId = settings.DefaultEmailConnectorId,
            DefaultEmailPipelineId = settings.DefaultEmailPipelineId,
            ProposalHtmlTemplate = settings.ProposalHtmlTemplate
        };
    }
}
