using AgencyCampaign.Application.Localization;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Application.Integrations;
using Archon.Application.Services;
using Microsoft.Extensions.Localization;
using Moq;

namespace AgencyCampaign.Testing.TestSupport
{
    public static class IntegrationPlatformClientFactory
    {
        public static IntegrationPlatformClient CreateInert()
        {
            Mock<IIntegrationService> integrationService = new();
            integrationService
                .Setup(item => item.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Integration?)null);

            Mock<IStringLocalizer<AgencyCampaignResource>> localizer = new();

            return new IntegrationPlatformClient(new Archon.Infrastructure.RestApi.RestApi(new HttpClient()), integrationService.Object, localizer.Object);
        }
    }
}
