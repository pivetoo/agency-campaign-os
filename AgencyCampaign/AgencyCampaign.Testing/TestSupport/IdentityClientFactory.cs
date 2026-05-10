using Archon.Application.Integrations;
using Archon.Application.Services;
using Archon.Infrastructure.IdentityManagement;
using Archon.Infrastructure.RestApi;
using Moq;

namespace AgencyCampaign.Testing.TestSupport
{
    public static class IdentityClientFactory
    {
        public static IdentityUsersClient CreateInert()
        {
            Mock<IIntegrationService> integrationService = new();
            integrationService
                .Setup(item => item.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Integration?)null);

            return new IdentityUsersClient(new Archon.Infrastructure.RestApi.RestApi(new HttpClient()), integrationService.Object);
        }
    }
}
