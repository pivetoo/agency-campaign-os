using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.ValueObjects
{
    [TestFixture]
    public sealed class PublicLinkTokenTests
    {
        [Test]
        public void Compose_should_prefix_tenant_when_present()
        {
            PublicLinkToken.Compose("tenant-1", "abc123").Should().Be("tenant-1~abc123");
        }

        [Test]
        public void Compose_should_return_random_only_when_tenant_blank()
        {
            PublicLinkToken.Compose(null, "abc123").Should().Be("abc123");
            PublicLinkToken.Compose("  ", "abc123").Should().Be("abc123");
        }

        [Test]
        public void ExtractTenantId_should_return_prefix_before_separator()
        {
            PublicLinkToken.ExtractTenantId("tenant-1~abc123").Should().Be("tenant-1");
        }

        [Test]
        public void ExtractTenantId_should_return_null_when_no_separator_or_blank()
        {
            PublicLinkToken.ExtractTenantId("abc123").Should().BeNull();
            PublicLinkToken.ExtractTenantId(null).Should().BeNull();
            PublicLinkToken.ExtractTenantId("").Should().BeNull();
        }

        [Test]
        public void Compose_and_Extract_should_roundtrip()
        {
            string token = PublicLinkToken.Compose("acme-tenant", "Zx-9_aBc");
            PublicLinkToken.ExtractTenantId(token).Should().Be("acme-tenant");
        }
    }
}
