using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignTests
    {
        [Test]
        public void Constructor_should_initialize_with_draft_status_and_active_state()
        {
            Campaign campaign = new(brandId: 1, name: "  Verão 2026  ", budget: 1000m, startsAt: DateTimeOffset.Now);

            campaign.Name.Should().Be("Verão 2026");
            campaign.Status.Should().Be(CampaignStatus.Draft);
            campaign.IsActive.Should().BeTrue();
            campaign.StartsAt.Offset.Should().Be(TimeSpan.Zero);
            campaign.Budget.Should().Be(1000m);
        }

        [Test]
        public void Constructor_should_normalize_optional_strings()
        {
            Campaign campaign = new(1, "Camp", 0, DateTimeOffset.Now,
                description: "  desc  ",
                objective: "",
                briefing: "  brief  ",
                internalOwnerName: "   ",
                notes: "n");

            campaign.Description.Should().Be("desc");
            campaign.Objective.Should().BeNull();
            campaign.Briefing.Should().Be("brief");
            campaign.InternalOwnerName.Should().BeNull();
            campaign.Notes.Should().Be("n");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_should_reject_invalid_brand(long brandId)
        {
            Action act = () => _ = new Campaign(brandId, "x", 0, DateTimeOffset.Now);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_should_reject_blank_name(string value)
        {
            Action act = () => _ = new Campaign(1, value, 0, DateTimeOffset.Now);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Constructor_should_reject_negative_budget()
        {
            Action act = () => _ = new Campaign(1, "x", -1m, DateTimeOffset.Now);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase(CampaignStatus.Cancelled)]
        [TestCase(CampaignStatus.Completed)]
        public void ChangeStatus_to_terminal_status_should_deactivate_campaign(CampaignStatus terminal)
        {
            Campaign campaign = new(1, "x", 0, DateTimeOffset.Now);

            campaign.ChangeStatus(terminal);

            campaign.Status.Should().Be(terminal);
            campaign.IsActive.Should().BeFalse();
        }

        [TestCase(CampaignStatus.Planned)]
        [TestCase(CampaignStatus.InProgress)]
        [TestCase(CampaignStatus.InReview)]
        public void ChangeStatus_to_non_terminal_status_should_keep_active(CampaignStatus status)
        {
            Campaign campaign = new(1, "x", 0, DateTimeOffset.Now);

            campaign.ChangeStatus(status);

            campaign.Status.Should().Be(status);
            campaign.IsActive.Should().BeTrue();
        }

        [Test]
        public void Update_should_replace_status_and_active_state()
        {
            Campaign campaign = new(1, "x", 0, DateTimeOffset.Now);

            campaign.Update(2, "y", 100m, DateTimeOffset.Now, null, null, null, null, CampaignStatus.InProgress, null, null, false);

            campaign.BrandId.Should().Be(2);
            campaign.Name.Should().Be("y");
            campaign.Status.Should().Be(CampaignStatus.InProgress);
            campaign.IsActive.Should().BeFalse();
        }
    }
}
