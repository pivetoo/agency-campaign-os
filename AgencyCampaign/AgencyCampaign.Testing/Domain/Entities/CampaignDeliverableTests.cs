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
        public void Constructor_should_reject_due_date_in_the_past()
        {
            Action act = () => BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(-1));
            act.Should().Throw<InvalidOperationException>().WithMessage("campaignDeliverable.dueAtInPast");
        }

        [Test]
        public void Constructor_should_accept_due_date_today()
        {
            Action act = () => BuildDefault(dueAt: DateTimeOffset.UtcNow);
            act.Should().NotThrow();
        }

        [Test]
        public void Update_should_reject_moving_due_date_to_the_past()
        {
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(5));

            Action act = () => subject.Update("Story 1", 3, 4, DateTimeOffset.UtcNow.AddDays(-2), 1000m, 800m, 100m, null, null);

            act.Should().Throw<InvalidOperationException>().WithMessage("campaignDeliverable.dueAtInPast");
        }

        [Test]
        public void Update_should_allow_editing_when_due_date_is_unchanged_even_if_already_past()
        {
            // Entregavel ja atrasado: editar outros campos sem mexer no prazo deve ser permitido.
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddHours(2));
            DateTimeOffset originalDue = subject.DueAt;

            Action act = () => subject.Update("Novo titulo", 3, 4, originalDue, 1000m, 800m, 100m, null, "nota");

            act.Should().NotThrow();
        }

        [Test]
        public void PromoteToApprovedFromContent_should_advance_pending_to_approved()
        {
            CampaignDeliverable subject = BuildDefault();

            subject.PromoteToApprovedFromContent();

            subject.Status.Should().Be(DeliverableStatus.Approved);
        }

        [Test]
        public void PromoteToApprovedFromContent_should_advance_in_review_to_approved()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.ChangeStatus(DeliverableStatus.InReview);

            subject.PromoteToApprovedFromContent();

            subject.Status.Should().Be(DeliverableStatus.Approved);
        }

        [Test]
        public void PromoteToApprovedFromContent_should_not_revert_published()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.Publish("https://x", null, DateTimeOffset.UtcNow);

            subject.PromoteToApprovedFromContent();

            subject.Status.Should().Be(DeliverableStatus.Published);
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
        public void ChangeStatus_should_allow_transitions_among_active_states()
        {
            CampaignDeliverable subject = BuildDefault();

            subject.ChangeStatus(DeliverableStatus.InReview);
            subject.Status.Should().Be(DeliverableStatus.InReview);
            subject.ChangeStatus(DeliverableStatus.Approved);
            subject.Status.Should().Be(DeliverableStatus.Approved);
            subject.ChangeStatus(DeliverableStatus.Pending);
            subject.Status.Should().Be(DeliverableStatus.Pending);
        }

        [Test]
        public void ChangeStatus_should_block_reverting_a_published_deliverable_to_active()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.Publish("https://x", null, DateTimeOffset.UtcNow);

            Action toApproved = () => subject.ChangeStatus(DeliverableStatus.Approved);
            Action toPending = () => subject.ChangeStatus(DeliverableStatus.Pending);

            toApproved.Should().Throw<InvalidOperationException>();
            toPending.Should().Throw<InvalidOperationException>();
            subject.Status.Should().Be(DeliverableStatus.Published);
        }

        [Test]
        public void ChangeStatus_should_block_reopening_a_cancelled_deliverable()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.ChangeStatus(DeliverableStatus.Cancelled);

            Action reopen = () => subject.ChangeStatus(DeliverableStatus.Pending);

            reopen.Should().Throw<InvalidOperationException>();
            subject.Status.Should().Be(DeliverableStatus.Cancelled);
        }

        [Test]
        public void Publish_should_be_blocked_from_a_cancelled_deliverable()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.ChangeStatus(DeliverableStatus.Cancelled);

            Action act = () => subject.Publish("https://x", null, DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>();
        }

        // Entregavel que nasceu com prazo valido e venceu com o tempo. O construtor (corretamente) recusa
        // prazo no passado, entao forcamos o estado via reflexao para exercitar o getter de SLA.
        private static CampaignDeliverable BuildOverdue(int daysPast = 2)
        {
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(1));
            typeof(CampaignDeliverable).GetProperty(nameof(CampaignDeliverable.DueAt))!
                .SetValue(subject, DateTimeOffset.UtcNow.AddDays(-daysPast));
            return subject;
        }

        [Test]
        public void SlaStatus_should_be_overdue_when_past_due_and_not_published()
        {
            CampaignDeliverable subject = BuildOverdue();
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
            CampaignDeliverable subject = BuildOverdue();
            subject.Publish("https://x", null, DateTimeOffset.UtcNow);

            subject.SlaStatus.Should().Be(DeliverableSlaStatus.Ok);
        }

        [Test]
        public void SlaStatus_should_be_ok_when_far_future()
        {
            CampaignDeliverable subject = BuildDefault(dueAt: DateTimeOffset.UtcNow.AddDays(30));
            subject.SlaStatus.Should().Be(DeliverableSlaStatus.Ok);
        }

        [Test]
        public void RegisterMetrics_should_store_values_source_and_timestamp()
        {
            CampaignDeliverable subject = BuildDefault();

            subject.RegisterMetrics(likes: 100, comments: 20, views: 5000, reach: 4000, impressions: 6000, saves: 30, shares: 10, source: DeliverableMetricsSource.Manual);

            subject.Likes.Should().Be(100);
            subject.Comments.Should().Be(20);
            subject.Views.Should().Be(5000);
            subject.Reach.Should().Be(4000);
            subject.Impressions.Should().Be(6000);
            subject.Saves.Should().Be(30);
            subject.Shares.Should().Be(10);
            subject.MetricsSource.Should().Be(DeliverableMetricsSource.Manual);
            subject.MetricsCollectedAt.Should().NotBeNull();
        }

        [Test]
        public void RegisterMetrics_should_compute_engagement_rate_over_reach()
        {
            CampaignDeliverable subject = BuildDefault();

            // interacoes = 100+20+10+30 = 160; reach 4000 -> 4,00%
            subject.RegisterMetrics(100, 20, null, 4000, 6000, 30, 10, DeliverableMetricsSource.Manual);

            subject.EngagementRate.Should().Be(4.00m);
        }

        [Test]
        public void RegisterMetrics_should_fall_back_to_impressions_when_reach_missing()
        {
            CampaignDeliverable subject = BuildDefault();

            // interacoes = 100+20 = 120; impressions 6000 -> 2,00%
            subject.RegisterMetrics(100, 20, null, null, 6000, null, null, DeliverableMetricsSource.Manual);

            subject.EngagementRate.Should().Be(2.00m);
        }

        [Test]
        public void RegisterMetrics_should_leave_engagement_rate_null_without_denominator()
        {
            CampaignDeliverable subject = BuildDefault();

            subject.RegisterMetrics(100, 20, 5000, null, null, 30, 10, DeliverableMetricsSource.Manual);

            subject.EngagementRate.Should().BeNull();
        }

        [Test]
        public void RegisterMetrics_should_reject_negative_values()
        {
            CampaignDeliverable subject = BuildDefault();

            Action act = () => subject.RegisterMetrics(-1, null, null, null, null, null, null, DeliverableMetricsSource.Manual);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void RegisterCreatorInsights_should_set_insights_preserving_public_metrics()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.RegisterMetrics(100, 20, 5000, null, null, null, 10, DeliverableMetricsSource.Manual);

            subject.RegisterCreatorInsights(reach: 4000, impressions: 6000, saves: 30);

            subject.Likes.Should().Be(100);
            subject.Comments.Should().Be(20);
            subject.Views.Should().Be(5000);
            subject.Shares.Should().Be(10);
            subject.Reach.Should().Be(4000);
            subject.Impressions.Should().Be(6000);
            subject.Saves.Should().Be(30);
            // interacoes = 100+20+10+30 = 160; reach 4000 -> 4,00%
            subject.EngagementRate.Should().Be(4.00m);
        }

        [Test]
        public void RegisterCreatorInsights_should_mark_mixed_when_public_was_auto()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.RegisterMetrics(100, 20, 5000, null, null, null, 10, DeliverableMetricsSource.Auto);

            subject.RegisterCreatorInsights(4000, 6000, 30);

            subject.MetricsSource.Should().Be(DeliverableMetricsSource.Mixed);
        }

        [Test]
        public void RegisterPublicMetrics_should_set_public_and_mark_auto_without_insights()
        {
            CampaignDeliverable subject = BuildDefault();

            subject.RegisterPublicMetrics(100, 20, 5000, 10);

            subject.Likes.Should().Be(100);
            subject.Comments.Should().Be(20);
            subject.Views.Should().Be(5000);
            subject.Shares.Should().Be(10);
            subject.MetricsSource.Should().Be(DeliverableMetricsSource.Auto);
        }

        [Test]
        public void RegisterPublicMetrics_should_mark_mixed_and_preserve_creator_insights()
        {
            CampaignDeliverable subject = BuildDefault();
            subject.RegisterCreatorInsights(4000, 6000, 30);

            subject.RegisterPublicMetrics(100, 20, 5000, 10);

            subject.Reach.Should().Be(4000);
            subject.Impressions.Should().Be(6000);
            subject.Saves.Should().Be(30);
            subject.Likes.Should().Be(100);
            subject.MetricsSource.Should().Be(DeliverableMetricsSource.Mixed);
            // interacoes = 100+20+10+30 = 160; reach 4000 -> 4,00%
            subject.EngagementRate.Should().Be(4.00m);
        }
    }
}
