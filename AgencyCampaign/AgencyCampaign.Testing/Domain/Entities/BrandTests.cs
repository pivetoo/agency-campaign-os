using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class BrandTests
    {
        [Test]
        public void Constructor_should_trim_name_and_normalize_optional_fields()
        {
            Brand brand = new("  Acme  ", tradeName: "  Acme Trade  ", document: "  12345678000199  ", contactName: " ", contactEmail: "info@acme.com", notes: "");

            brand.Name.Should().Be("Acme");
            brand.TradeName.Should().Be("Acme Trade");
            brand.Document.Should().Be("12345678000199");
            brand.ContactName.Should().BeNull();
            brand.ContactEmail.Should().Be("info@acme.com");
            brand.Notes.Should().BeNull();
            brand.IsActive.Should().BeTrue();
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Constructor_should_reject_blank_name(string? value)
        {
            Action act = () => _ = new Brand(value!);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_should_replace_fields_and_toggle_active_state()
        {
            Brand brand = new("Acme");

            brand.Update("New Name", tradeName: "trade", document: "doc", contactName: "Jane", contactEmail: "jane@acme.com", notes: "n", isActive: false);

            brand.Name.Should().Be("New Name");
            brand.TradeName.Should().Be("trade");
            brand.IsActive.Should().BeFalse();
        }

        [Test]
        public void Update_should_reject_blank_name()
        {
            Brand brand = new("Acme");
            Action act = () => brand.Update("  ", null, null, null, null, null, true);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SetLogo_should_normalize_input()
        {
            Brand brand = new("Acme");

            brand.SetLogo("  https://logo  ");
            brand.LogoUrl.Should().Be("https://logo");

            brand.SetLogo(null);
            brand.LogoUrl.Should().BeNull();

            brand.SetLogo("   ");
            brand.LogoUrl.Should().BeNull();
        }
    }
}
