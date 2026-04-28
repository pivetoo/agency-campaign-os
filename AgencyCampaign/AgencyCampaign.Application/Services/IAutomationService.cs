using AgencyCampaign.Application.Requests.Automations;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IAutomationService : ICrudService<Automation>
    {
        Task<PagedResult<Automation>> GetAutomations(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Automation?> GetAutomationById(long id, CancellationToken cancellationToken = default);

        Task<Automation> CreateAutomation(CreateAutomationRequest request, CancellationToken cancellationToken = default);

        Task<Automation> UpdateAutomation(long id, UpdateAutomationRequest request, CancellationToken cancellationToken = default);
    }
}
