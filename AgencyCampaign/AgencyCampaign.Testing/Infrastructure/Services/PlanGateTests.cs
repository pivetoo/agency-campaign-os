using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using Archon.Core.Exceptions;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class PlanGateTests
    {
        private sealed class StubTierResolver : IPlanTierResolver
        {
            private readonly PlanTier tier;

            public StubTierResolver(PlanTier tier)
            {
                this.tier = tier;
            }

            public Task<PlanTier> GetCurrentTierAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(tier);
            }
        }

        private static PlanGate CreateGate(PlanTier tier)
        {
            return new PlanGate(new StubTierResolver(tier), new EntitlementService());
        }

        [Test]
        public async Task RequireFeature_should_throw_forbidden_when_tier_lacks_the_feature()
        {
            PlanGate subject = CreateGate(PlanTier.Essencial);

            Func<Task> act = () => subject.RequireFeatureAsync(PlanFeature.DigitalSignature);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Test]
        public async Task RequireFeature_should_pass_when_tier_has_the_feature()
        {
            PlanGate subject = CreateGate(PlanTier.Pro);

            Func<Task> act = () => subject.RequireFeatureAsync(PlanFeature.DigitalSignature);

            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task HasFeature_should_not_throw_and_reflect_the_tier()
        {
            PlanGate essencial = CreateGate(PlanTier.Essencial);
            PlanGate scale = CreateGate(PlanTier.Scale);

            (await essencial.HasFeatureAsync(PlanFeature.ApifySync)).Should().BeFalse();
            (await scale.HasFeatureAsync(PlanFeature.ApifySync)).Should().BeTrue();
        }

        [Test]
        public async Task RequireWithinLimit_should_throw_when_usage_reaches_the_cap()
        {
            PlanGate subject = CreateGate(PlanTier.Essencial);

            Func<Task> below = () => subject.RequireWithinLimitAsync(PlanLimit.ActiveManagedCreators, 49);
            Func<Task> atCap = () => subject.RequireWithinLimitAsync(PlanLimit.ActiveManagedCreators, 50);

            await below.Should().NotThrowAsync();
            await atCap.Should().ThrowAsync<ForbiddenException>();
        }

        [Test]
        public async Task Internal_tenant_should_never_be_gated()
        {
            PlanGate subject = CreateGate(PlanTier.Internal);

            Func<Task> feature = () => subject.RequireFeatureAsync(PlanFeature.ApifySync);
            Func<Task> limit = () => subject.RequireWithinLimitAsync(PlanLimit.ActiveManagedCreators, 1_000_000);

            await feature.Should().NotThrowAsync();
            await limit.Should().NotThrowAsync();
        }
    }
}
