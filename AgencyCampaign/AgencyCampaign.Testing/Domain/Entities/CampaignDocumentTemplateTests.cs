using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignDocumentTemplateTests
    {
        [Test]
        public void Constructor_should_trim_name_and_keep_body_intact()
        {
            CampaignDocumentTemplate subject = new("  Padrão  ", CampaignDocumentType.CreatorAgreement, body: "  corpo com espaços  ");

            subject.Name.Should().Be("Padrão");
            subject.Body.Should().Be("  corpo com espaços  ");
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_blank_name_or_body()
        {
            Action blankName = () => _ = new CampaignDocumentTemplate(" ", CampaignDocumentType.CreatorAgreement, "x");
            Action blankBody = () => _ = new CampaignDocumentTemplate("x", CampaignDocumentType.CreatorAgreement, "  ");

            blankName.Should().Throw<ArgumentException>();
            blankBody.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Activate_and_Deactivate_should_toggle_active_state()
        {
            CampaignDocumentTemplate subject = new("x", CampaignDocumentType.CreatorAgreement, "body");

            subject.Deactivate();
            subject.IsActive.Should().BeFalse();

            subject.Activate();
            subject.IsActive.Should().BeTrue();
        }

        [Test]
        public void Update_should_replace_state()
        {
            CampaignDocumentTemplate subject = new("x", CampaignDocumentType.CreatorAgreement, "body");

            subject.Update("y", CampaignDocumentType.BrandContract, "novo body", "desc", false);

            subject.Name.Should().Be("y");
            subject.DocumentType.Should().Be(CampaignDocumentType.BrandContract);
            subject.IsActive.Should().BeFalse();
            subject.Body.Should().Be("novo body");
        }
    }
}
