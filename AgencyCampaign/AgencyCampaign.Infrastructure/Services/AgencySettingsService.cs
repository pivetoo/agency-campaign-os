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
                request.PrimaryColor);

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

        public async Task<IReadOnlyList<ProposalTemplateVersionModel>> GetProposalTemplateVersions(CancellationToken cancellationToken = default)
        {
            List<ProposalTemplateVersion> versions = await dbContext.Set<ProposalTemplateVersion>()
                .AsNoTracking()
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);

            return versions.Select(MapVersion).ToList();
        }

        public async Task<ProposalTemplateVersionModel> SaveProposalTemplateVersion(string name, string template, bool activate, CancellationToken cancellationToken = default)
        {
            ProposalTemplateVersion version = new(name, template);
            dbContext.Set<ProposalTemplateVersion>().Add(version);

            if (activate)
            {
                await DeactivateAllVersions(cancellationToken);
                version.Activate();
                AgencySettings settings = await ResolveOrCreate(cancellationToken);
                settings.SetProposalHtmlTemplate(template);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return MapVersion(version);
        }

        public async Task<ProposalTemplateVersionModel> ActivateProposalTemplateVersion(long id, CancellationToken cancellationToken = default)
        {
            ProposalTemplateVersion? version = await dbContext.Set<ProposalTemplateVersion>()
                .AsTracking()
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

            if (version is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await DeactivateAllVersions(cancellationToken);
            version.Activate();

            AgencySettings settings = await ResolveOrCreate(cancellationToken);
            settings.SetProposalHtmlTemplate(version.Template);

            await dbContext.SaveChangesAsync(cancellationToken);
            return MapVersion(version);
        }

        public async Task DeleteProposalTemplateVersion(long id, CancellationToken cancellationToken = default)
        {
            ProposalTemplateVersion? version = await dbContext.Set<ProposalTemplateVersion>()
                .AsTracking()
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

            if (version is null)
            {
                return;
            }

            dbContext.Set<ProposalTemplateVersion>().Remove(version);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task DeactivateAllVersions(CancellationToken cancellationToken)
        {
            List<ProposalTemplateVersion> all = await dbContext.Set<ProposalTemplateVersion>()
                .AsTracking()
                .Where(v => v.IsActive)
                .ToListAsync(cancellationToken);

            foreach (ProposalTemplateVersion v in all)
            {
                v.Deactivate();
            }
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

        private static ProposalTemplateVersionModel MapVersion(ProposalTemplateVersion version) => new()
        {
            Id = version.Id,
            Name = version.Name,
            Template = version.Template,
            IsActive = version.IsActive,
            CreatedAt = version.CreatedAt,
        };

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
            ProposalHtmlTemplate = settings.ProposalHtmlTemplate
        };
    }
}
