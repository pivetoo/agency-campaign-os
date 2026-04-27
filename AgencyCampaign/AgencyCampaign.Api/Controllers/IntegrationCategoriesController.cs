using AgencyCampaign.Infrastructure.Clients;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class IntegrationCategoriesController : ApiControllerBase
    {
        private readonly IntegrationPlataformClient integrationPlataformClient;

        public IntegrationCategoriesController(IntegrationPlataformClient integrationPlataformClient)
        {
            this.integrationPlataformClient = integrationPlataformClient;
        }

        [RequireAccess("Permite listar as categorias de integracao disponiveis.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<IntegrationCategoryDto> categories = await integrationPlataformClient.GetActiveIntegrationCategoriesAsync(cancellationToken);
            return Http200(categories);
        }
    }
}
