using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CreatorTests
    {
        [Test]
        public void Constructor_should_trim_name_and_normalize_optional_strings()
        {
            Creator creator = new(
                "  Foo Creator  ",
                stageName: "  Foo  ",
                email: "  foo@bar.com  ",
                phone: " ",
                document: "12345",
                pixKey: "  pix  ",
                pixKeyType: PixKeyType.Email,
                primaryNiche: "fashion",
                city: "  SP  ",
                state: "SP",
                notes: "",
                defaultAgencyFeePercent: 15m);

            creator.Name.Should().Be("Foo Creator");
            creator.StageName.Should().Be("Foo");
            creator.Email.Should().Be("foo@bar.com");
            creator.Phone.Should().BeNull();
            creator.PixKey.Should().Be("pix");
            creator.PixKeyType.Should().Be(PixKeyType.Email);
            creator.City.Should().Be("SP");
            creator.Notes.Should().BeNull();
            creator.DefaultAgencyFeePercent.Should().Be(15m);
            creator.IsActive.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_blank_name()
        {
            Action act = () => _ = new Creator("   ");
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Constructor_should_reject_negative_agency_fee_percent()
        {
            Action act = () => _ = new Creator("Foo", defaultAgencyFeePercent: -1m);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Update_should_replace_fields_and_toggle_active_state()
        {
            Creator creator = new("Foo");

            creator.Update("Bar", null, null, null, null, "pix", PixKeyType.Cpf, null, null, null, null, 10m, false);

            creator.Name.Should().Be("Bar");
            creator.PixKey.Should().Be("pix");
            creator.PixKeyType.Should().Be(PixKeyType.Cpf);
            creator.DefaultAgencyFeePercent.Should().Be(10m);
            creator.IsActive.Should().BeFalse();
        }
    }
}
