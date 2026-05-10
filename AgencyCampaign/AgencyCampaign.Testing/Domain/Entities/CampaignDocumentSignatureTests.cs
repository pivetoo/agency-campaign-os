using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignDocumentSignatureTests
    {
        [Test]
        public void Constructor_should_trim_signer_data()
        {
            CampaignDocumentSignature subject = new(1, CampaignDocumentSignerRole.Creator, "  Foo  ", "  foo@x  ", " 12345 ", " p-1 ");

            subject.SignerName.Should().Be("Foo");
            subject.SignerEmail.Should().Be("foo@x");
            subject.SignerDocumentNumber.Should().Be("12345");
            subject.ProviderSignerId.Should().Be("p-1");
            subject.IsSigned.Should().BeFalse();
        }

        [Test]
        public void Constructor_should_reject_blank_name_or_email()
        {
            Action blankName = () => _ = new CampaignDocumentSignature(1, CampaignDocumentSignerRole.Creator, " ", "x", null, null);
            Action blankEmail = () => _ = new CampaignDocumentSignature(1, CampaignDocumentSignerRole.Creator, "x", " ", null, null);

            blankName.Should().Throw<ArgumentException>();
            blankEmail.Should().Throw<ArgumentException>();
        }

        [Test]
        public void MarkSigned_should_be_idempotent()
        {
            CampaignDocumentSignature subject = new(1, CampaignDocumentSignerRole.Creator, "Foo", "foo@x", null, null);
            DateTimeOffset firstSign = DateTimeOffset.UtcNow.AddMinutes(-5);

            subject.MarkSigned(firstSign, "1.1.1.1", "ua");
            subject.MarkSigned(DateTimeOffset.UtcNow, "2.2.2.2", "other");

            subject.SignedAt.Should().Be(firstSign);
            subject.IpAddress.Should().Be("1.1.1.1");
            subject.UserAgent.Should().Be("ua");
        }

        [Test]
        public void AssignProviderSignerId_should_reject_blank()
        {
            CampaignDocumentSignature subject = new(1, CampaignDocumentSignerRole.Creator, "Foo", "foo@x", null, null);
            Action act = () => subject.AssignProviderSignerId(" ");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void AssignProviderSignerId_should_persist_value()
        {
            CampaignDocumentSignature subject = new(1, CampaignDocumentSignerRole.Creator, "Foo", "foo@x", null, null);
            subject.AssignProviderSignerId(" p-1 ");
            subject.ProviderSignerId.Should().Be("p-1");
        }
    }
}
