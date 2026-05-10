using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Testing.Builders;
using CampaignCreatorStatus = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CampaignCreatorTests
    {
        [Test]
        public void Constructor_should_calculate_agency_fee_amount()
        {
            CampaignCreator subject = new(campaignId: 1, creatorId: 2, campaignCreatorStatusId: 3,
                agreedAmount: 1000m, agencyFeePercent: 12.5m);

            subject.AgencyFeeAmount.Should().Be(125.00m);
        }

        [Test]
        public void Constructor_should_round_agency_fee_using_away_from_zero()
        {
            CampaignCreator subject = new(1, 2, 3, agreedAmount: 100m, agencyFeePercent: 12.345m);

            // 100 * 12.345 / 100 = 12.345 => round to 12.35
            subject.AgencyFeeAmount.Should().Be(12.35m);
        }

        [Test]
        public void Update_should_recalculate_fee_with_existing_percent()
        {
            CampaignCreator subject = new(1, 2, 3, agreedAmount: 100m, agencyFeePercent: 10m);

            subject.Update(agreedAmount: 500m, notes: "  ok  ");

            subject.AgreedAmount.Should().Be(500m);
            subject.AgencyFeeAmount.Should().Be(50m);
            subject.Notes.Should().Be("ok");
        }

        [Test]
        public void ChangeStatus_to_status_that_marks_confirmed_should_set_confirmed_at_once()
        {
            CampaignCreator subject = new(1, 2, 3, 100m, 10m);
            CampaignCreatorStatus confirmed = new CampaignCreatorStatusBuilder()
                .WithId(10)
                .WithCategory(CampaignCreatorStatusCategory.Success)
                .MarksAsConfirmed()
                .Build();
            DateTimeOffset firstTimestamp = DateTimeOffset.UtcNow.AddMinutes(-1);

            subject.ChangeStatus(confirmed, firstTimestamp);

            subject.CampaignCreatorStatusId.Should().Be(10);
            subject.ConfirmedAt.Should().Be(firstTimestamp);

            // moving to another confirmed status must keep the original confirmed timestamp
            CampaignCreatorStatus anotherConfirmed = new CampaignCreatorStatusBuilder()
                .WithId(11)
                .WithCategory(CampaignCreatorStatusCategory.Success)
                .MarksAsConfirmed()
                .Build();

            subject.ChangeStatus(anotherConfirmed, DateTimeOffset.UtcNow);

            subject.ConfirmedAt.Should().Be(firstTimestamp);
            subject.CancelledAt.Should().BeNull();
        }

        [Test]
        public void ChangeStatus_to_failure_category_should_set_cancelled_at()
        {
            CampaignCreator subject = new(1, 2, 3, 100m, 10m);
            CampaignCreatorStatus cancelled = new CampaignCreatorStatusBuilder()
                .WithId(20)
                .WithCategory(CampaignCreatorStatusCategory.Failure)
                .Build();

            subject.ChangeStatus(cancelled);

            subject.CampaignCreatorStatusId.Should().Be(20);
            subject.CancelledAt.Should().NotBeNull();
        }

        [Test]
        public void ChangeStatus_to_in_progress_status_should_clear_cancelled_at()
        {
            CampaignCreator subject = new(1, 2, 3, 100m, 10m);

            CampaignCreatorStatus cancelled = new CampaignCreatorStatusBuilder()
                .WithId(20)
                .WithCategory(CampaignCreatorStatusCategory.Failure)
                .Build();
            subject.ChangeStatus(cancelled);
            subject.CancelledAt.Should().NotBeNull();

            CampaignCreatorStatus inProgress = new CampaignCreatorStatusBuilder()
                .WithId(21)
                .WithCategory(CampaignCreatorStatusCategory.InProgress)
                .Build();
            subject.ChangeStatus(inProgress);

            subject.CancelledAt.Should().BeNull();
        }

        [Test]
        public void ChangeStatus_should_reject_null_status()
        {
            CampaignCreator subject = new(1, 2, 3, 100m, 10m);
            Action act = () => subject.ChangeStatus(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_reject_invalid_amounts()
        {
            Action negative = () => _ = new CampaignCreator(1, 2, 3, -1m, 10m);
            Action negativeFee = () => _ = new CampaignCreator(1, 2, 3, 100m, -1m);

            negative.Should().Throw<ArgumentOutOfRangeException>();
            negativeFee.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
