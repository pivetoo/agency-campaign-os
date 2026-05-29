using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class BrandContactTests
    {
        [Test]
        public void Constructor_should_normalize_and_store()
        {
            BrandContact subject = new(5, BrandContactType.Email, " a@x.com ", " comercial ", true);

            subject.BrandId.Should().Be(5);
            subject.Type.Should().Be(BrandContactType.Email);
            subject.Value.Should().Be("a@x.com");
            subject.Label.Should().Be("comercial");
            subject.IsPrimary.Should().BeTrue();
        }

        [Test]
        public void Constructor_should_reject_invalid()
        {
            Action invalidBrand = () => _ = new BrandContact(0, BrandContactType.Email, "a@x.com", null, true);
            Action emptyValue = () => _ = new BrandContact(5, BrandContactType.Phone, "  ", null, false);

            invalidBrand.Should().Throw<ArgumentOutOfRangeException>();
            emptyValue.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SetPrimary_toggles()
        {
            BrandContact subject = new(5, BrandContactType.Phone, "11999", null, false);

            subject.SetPrimary(true);
            subject.IsPrimary.Should().BeTrue();
            subject.SetPrimary(false);
            subject.IsPrimary.Should().BeFalse();
        }
    }
}
