using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignDeliverableTests
    {
        private static CampaignDeliverable BuildDefault(decimal gross = 1000m, decimal creator = 800m, decimal fee = 100m, DateTimeOffset? dueAt = null)
        {
            return new CampaignDeliverable(
                campaignId: 1,
                campaignCreatorId: 2,
                title: "Story 1",
                deliverableKindId: 3,
                platformId: 4,
                dueAt: dueAt ?? DateTimeOffset.UtcNow.AddDays(10),
                grossAmount: gross,
                creatorAmount: creator,
                agencyFeeAmount: fee);
        }

        [Test]
        public void Constructor_should_initialize_with_pending_status_and_amounts()
        {
            CampaignDeliverable subject = BuildDefault();

            subject.Status.Should().Be(DeliverableStatus.Pending);
            subject.GrossAmount.Should().Be(1000m);
            subject.CreatorAmount.Should().Be(800m);
            subject.AgencyFeeAmount.Should().Be(100m);
        }

        [Test]
        public void Constructor_should_reject_when_creator_plus_fee_exceeds_gross()
        {
            Action act = () => BuildDefault(gross: 100m, creator: 80m, fee: 30m);
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Publish_should_set_status_and_url()
        {
            CampaignDeliverable subject = BuildDefault();
            DateTimeOffset publishedAt = DateTimeOffset.Now;

            subject.Publish("https://x", "https://evidence", publishedAt);

            subject.Status.Should().Be(DeliverableStatus.Published);
            subject.PublishedUrl.Should().Be("https://x");
            subject.EvidenceUrl.Should().Be("https://evidence");
            subject.PublishedAt!.Value.Offset.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void Publish_should_reject_blank_url()
        {
            CampaignDeliverable subject = BuildDefault();
            Action act = () => subject.Publish("   ", null, DateTimeOffset.UtcNow);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void ChangeStatus_to_non_published_should_clear_published_data()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.Publish("https://x", null, DateTimeOffset.UtcNow);

            subject.ChangeStatus(DeliverableStatus.Cancelled);

            subject.Status.Should().Be(DeliverableStatus.Cancelled);
            subject.PublishedAt.Should().BeNull();
            subject.PublishedUrl.Should().BeNull();
        }

        [Test]
        public void SlaStatus_should_be_overdue_when_past_due_and_not_published()
        {
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(-2));
            subject.SlaStatus.Should().Be(DeliverableSlaStatus.Overdue);
        }

        [Test]
        public void SlaStatus_should_be_due_soon_within_three_days()
        {
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddHours(36));
            subject.SlaStatus.Should().Be(DeliverableSlaStatus.DueSoon);
        }

        [Test]
        public void SlaStatus_should_be_ok_when_published_even_if_past_due()
        {
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(-2));
            subject.Publish("https://x", null, DateTimeOffset.UtcNow);

            subject.SlaStatus.Should().Be(DeliverableSlaStatus.Ok);
        }

        [Test]
        public void SlaStatus_should_be_ok_when_far_future()
        {
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(30));
            subject.SlaStatus.Should().Be(DeliverableSlaStatus.Ok);
        }
    }
}
