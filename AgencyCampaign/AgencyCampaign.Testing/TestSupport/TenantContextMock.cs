using Archon.Application.MultiTenancy;
using Moq;

namespace AgencyCampaign.Testing.TestSupport
{
    public static class TenantContextMock
    {
        public static ITenantContext Create(string? tenantId = "tenant-1")
        {
            Mock<ITenantContext> mock = new();
            mock.SetupGet(item => item.TenantId).Returns(tenantId);
            mock.SetupGet(item => item.HasTenant).Returns(!string.IsNullOrWhiteSpace(tenantId));
            return mock.Object;
        }
    }
}
