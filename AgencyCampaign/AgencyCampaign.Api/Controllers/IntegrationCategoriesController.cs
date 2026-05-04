using AgencyCampaign.Infrastructure.Clients;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class IntegrationCategoriesController : ApiControllerBase
    {
        private readonly IntegrationPlatformClient integrationPlatformClient;

        public IntegrationCategoriesController(IntegrationPlatformClient integrationPlatformClient)
        {
            this.integrationPlatformClient = integrationPlatformClient;
        }

        [RequireAccess("Permite listar as categorias de integracao disponiveis.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<IntegrationCategoryDto> categories = await integrationPlatformClient.GetActiveIntegrationCategoriesAsync(cancellationToken);
            return Http200(categories);
        }
    }
}
