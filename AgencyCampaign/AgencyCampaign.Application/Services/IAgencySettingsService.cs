using AgencyCampaign.Application.Models;
using AgencyCampaign.Application.Requests.AgencySettings;

namespace AgencyCampaign.Application.Services
{
    public interface IAgencySettingsService
    {
        Task<AgencySettingsModel> Get(CancellationToken cancellationToken = default);

        Task<AgencySettingsModel> Update(UpdateAgencySettingsRequest request, CancellationToken cancellationToken = default);

        Task<AgencySettingsModel> SetLogo(string logoUrl, CancellationToken cancellationToken = default);

        Task<AgencySettingsModel> RemoveLogo(CancellationToken cancellationToken = default);

        Task<AgencySettingsModel> SetDefaultEmailConnector(long? connectorId, CancellationToken cancellationToken = default);

        Task<AgencySettingsModel> SaveProposalTemplate(string? template, CancellationToken cancellationToken = default);

        Task<string> PreviewProposalTemplate(string template, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProposalLayoutModel>> GetProposalLayouts(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProposalTemplateVersionModel>> GetProposalTemplateVersions(CancellationToken cancellationToken = default);

        Task<ProposalTemplateVersionModel> SaveProposalTemplateVersion(string name, string template, bool activate, CancellationToken cancellationToken = default);

        Task<ProposalTemplateVersionModel> ActivateProposalTemplateVersion(long id, CancellationToken cancellationToken = default);

        Task DeleteProposalTemplateVersion(long id, CancellationToken cancellationToken = default);
    }
}
