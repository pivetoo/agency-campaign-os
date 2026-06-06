using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class EntitlementServiceTests
    {
        private static EntitlementService CreateSubject()
        {
            return new EntitlementService();
        }

        [Test]
        public void Essencial_should_not_have_any_gated_feature()
        {
            EntitlementService subject = CreateSubject();

            subject.HasFeature(PlanTier.Essencial, PlanFeature.DigitalSignature).Should().BeFalse();
            subject.HasFeature(PlanTier.Essencial, PlanFeature.PixPayout).Should().BeFalse();
            subject.HasFeature(PlanTier.Essencial, PlanFeature.ApifySync).Should().BeFalse();
        }

        [Test]
        public void Pro_should_have_pro_features_but_not_scale_features()
        {
            EntitlementService subject = CreateSubject();

            subject.HasFeature(PlanTier.Pro, PlanFeature.DigitalSignature).Should().BeTrue();
            subject.HasFeature(PlanTier.Pro, PlanFeature.Automations).Should().BeTrue();
            subject.HasFeature(PlanTier.Pro, PlanFeature.ApifySync).Should().BeFalse();
            subject.HasFeature(PlanTier.Pro, PlanFeature.EmvRoi).Should().BeFalse();
        }

        [Test]
        public void Scale_should_have_every_gated_feature()
        {
            EntitlementService subject = CreateSubject();

            foreach (PlanFeature feature in Enum.GetValues<PlanFeature>())
            {
                subject.HasFeature(PlanTier.Scale, feature).Should().BeTrue();
            }
        }

        [Test]
        public void Internal_tenant_should_have_everything_and_no_limit()
        {
            EntitlementService subject = CreateSubject();

            foreach (PlanFeature feature in Enum.GetValues<PlanFeature>())
            {
                subject.HasFeature(PlanTier.Internal, feature).Should().BeTrue();
            }

            subject.CheckLimit(PlanTier.Internal, PlanLimit.ActiveManagedCreators, 100_000).Allowed.Should().BeTrue();
        }

        [Test]
        public void CheckLimit_should_block_when_usage_reaches_the_tier_cap()
        {
            EntitlementService subject = CreateSubject();

            subject.CheckLimit(PlanTier.Essencial, PlanLimit.ActiveManagedCreators, 24).Allowed.Should().BeTrue();
            subject.CheckLimit(PlanTier.Essencial, PlanLimit.ActiveManagedCreators, 25).Allowed.Should().BeFalse();
            subject.CheckLimit(PlanTier.Pro, PlanLimit.ActiveManagedCreators, 99).Allowed.Should().BeTrue();
            subject.CheckLimit(PlanTier.Pro, PlanLimit.ActiveManagedCreators, 100).Allowed.Should().BeFalse();
        }

        [Test]
        public void Scale_active_campaigns_should_be_unlimited()
        {
            EntitlementService subject = CreateSubject();

            EntitlementCheck check = subject.CheckLimit(PlanTier.Scale, PlanLimit.ActiveCampaigns, 5_000);

            check.Allowed.Should().BeTrue();
            check.IsUnlimited.Should().BeTrue();
        }
    }
}
