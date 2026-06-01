using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.ValueObjects
{
    [TestFixture]
    public sealed class IpAnonymizerTests
    {
        [Test]
        public void Anonymize_should_zero_last_octet_of_ipv4()
        {
            IpAnonymizer.Anonymize("203.0.113.45").Should().Be("203.0.113.0");
        }

        [Test]
        public void Anonymize_should_zero_host_bits_of_ipv6()
        {
            IpAnonymizer.Anonymize("2001:db8::1").Should().Be("2001:db8::");
        }

        [Test]
        public void Anonymize_should_return_null_for_blank_or_invalid()
        {
            IpAnonymizer.Anonymize(null).Should().BeNull();
            IpAnonymizer.Anonymize(" ").Should().BeNull();
            IpAnonymizer.Anonymize("not-an-ip").Should().BeNull();
        }
    }
}
