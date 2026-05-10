using AgencyCampaign.Infrastructure.Clients;
using Archon.Application.Integrations;
using Archon.Application.Services;
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

            return new IntegrationPlatformClient(new Archon.Infrastructure.RestApi.RestApi(new HttpClient()), integrationService.Object);
        }
    }
}
