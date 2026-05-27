using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Domain
{
    [TestFixture]
    public sealed class DeliverableContentAssetTests
    {
        [Test]
        public void Constructor_trims_url_and_keeps_type()
        {
            DeliverableContentAsset asset = new(ContentAssetType.ExternalUrl, "  https://x/y  ", null, null, 0);

            asset.Url.Should().Be("https://x/y");
            asset.Type.Should().Be(ContentAssetType.ExternalUrl);
        }

        [Test]
        public void Constructor_rejects_empty_url()
        {
            Action act = () => new DeliverableContentAsset(ContentAssetType.ImageUpload, "  ", "f.png", "image/png", 0);

            act.Should().Throw<ArgumentException>();
        }
    }
}
