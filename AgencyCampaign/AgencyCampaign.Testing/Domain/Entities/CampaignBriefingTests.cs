using AgencyCampaign.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignBriefingTests
    {
        [Test]
        public void Constructor_should_normalize_and_store()
        {
            CampaignBriefing subject = new(10, " Mensagem ", " fazer ", " evitar ", " #tag ", " @perfil ", " http://ref ");

            subject.CampaignId.Should().Be(10);
            subject.KeyMessage.Should().Be("Mensagem");
            subject.Dos.Should().Be("fazer");
            subject.Donts.Should().Be("evitar");
            subject.Hashtags.Should().Be("#tag");
            subject.Mentions.Should().Be("@perfil");
            subject.ReferenceLinks.Should().Be("http://ref");
        }

        [Test]
        public void Constructor_should_reject_invalid_campaign()
        {
            Action act = () => _ = new CampaignBriefing(0, null, null, null, null, null, null);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Update_should_replace_and_normalize_values()
        {
            CampaignBriefing subject = new(10, "a", "b", "c", "d", "e", "f");

            subject.Update(" novo ", null, "  ", "x", null, null);

            subject.KeyMessage.Should().Be("novo");
            subject.Dos.Should().BeNull();
            subject.Donts.Should().BeNull();
            subject.Hashtags.Should().Be("x");
            subject.Mentions.Should().BeNull();
        }
    }
}
