using Archon.Application.Abstractions;
using Moq;

namespace AgencyCampaign.Testing.TestSupport
{
    public static class CurrentUserMock
    {
        public static ICurrentUser Create(long? userId = 1, string? userName = "Tester", string? email = "tester@x", string? clientId = "test-client", bool isAuthenticated = true)
        {
            Mock<ICurrentUser> mock = new();
            mock.SetupGet(item => item.IsAuthenticated).Returns(isAuthenticated);
            mock.SetupGet(item => item.UserId).Returns(userId);
            mock.SetupGet(item => item.UserName).Returns(userName);
            mock.SetupGet(item => item.Email).Returns(email);
            mock.SetupGet(item => item.ClientId).Returns(clientId);
            return mock.Object;
        }
    }
}
