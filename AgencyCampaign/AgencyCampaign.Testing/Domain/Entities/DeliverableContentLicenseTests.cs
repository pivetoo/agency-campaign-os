using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class DeliverableContentLicenseTests
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        private static DeliverableContentLicense License(DateTimeOffset? expiresAt)
        {
            return new DeliverableContentLicense(10, ContentLicenseType.UgcReuse, "Site, ads", null, expiresAt, 500m, "nota", null);
        }

        [Test]
        public void No_expiry_is_always_active()
        {
            License(null).ComputeStatus(Now, 30).Should().Be(ContentLicenseStatus.Active);
        }

        [Test]
        public void Far_expiry_is_active()
        {
            License(Now.AddDays(40)).ComputeStatus(Now, 30).Should().Be(ContentLicenseStatus.Active);
        }

        [Test]
        public void Near_expiry_is_expiring_soon()
        {
            License(Now.AddDays(5)).ComputeStatus(Now, 30).Should().Be(ContentLicenseStatus.ExpiringSoon);
        }

        [Test]
        public void Past_expiry_is_expired()
        {
            License(Now.AddDays(-1)).ComputeStatus(Now, 30).Should().Be(ContentLicenseStatus.Expired);
        }

        [Test]
        public void DaysUntilExpiry_rounds_up_and_floors_at_zero()
        {
            License(Now.AddDays(10)).DaysUntilExpiry(Now).Should().Be(10);
            License(Now.AddHours(36)).DaysUntilExpiry(Now).Should().Be(2);
            License(Now.AddDays(-5)).DaysUntilExpiry(Now).Should().Be(0);
            License(null).DaysUntilExpiry(Now).Should().BeNull();
        }

        [Test]
        public void IsExpired_reflects_expiry()
        {
            License(Now.AddDays(-1)).IsExpired(Now).Should().BeTrue();
            License(Now.AddDays(1)).IsExpired(Now).Should().BeFalse();
            License(null).IsExpired(Now).Should().BeFalse();
        }

        [Test]
        public void MarkAlerted_sets_threshold()
        {
            DeliverableContentLicense license = License(Now.AddDays(5));

            license.MarkAlerted(7);

            license.LastAlertedThresholdDays.Should().Be(7);
        }

        [Test]
        public void Constructor_rejects_negative_value()
        {
            Action act = () => new DeliverableContentLicense(10, ContentLicenseType.UgcReuse, null, null, null, -1m, null, null);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Update_resets_alert_when_expiry_changes()
        {
            DeliverableContentLicense license = License(Now.AddDays(5));
            license.MarkAlerted(7);

            license.Update(ContentLicenseType.UgcReuse, "Site", null, Now.AddDays(60), 500m, "nota", null);

            license.LastAlertedThresholdDays.Should().BeNull();
        }
    }
}
