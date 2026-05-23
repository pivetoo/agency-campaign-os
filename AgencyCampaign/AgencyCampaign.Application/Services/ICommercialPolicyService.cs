using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Commercial;

namespace AgencyCampaign.Application.Services
{
    public interface ICommercialPolicyService
    {
        Task<CommercialPolicyModel?> GetCurrent(CancellationToken cancellationToken = default);

        Task<CommercialPolicyModel> Upsert(UpsertCommercialPolicyRequest request, CancellationToken cancellationToken = default);
    }
}
