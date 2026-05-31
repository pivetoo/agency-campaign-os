using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CommercialPolicyTests
    {
        [Test]
        public void Constructor_should_reject_default_payment_term_above_max()
        {
            Action act = () => _ = new CommercialPolicy(maxDiscountPercent: null, defaultPaymentTermDays: 60, maxPaymentTermDays: 30);

            act.Should().Throw<InvalidOperationException>().WithMessage("commercialPolicy.paymentTerm.defaultExceedsMax");
        }

        [Test]
        public void Update_should_reject_default_payment_term_above_max()
        {
            CommercialPolicy policy = new(maxDiscountPercent: null, defaultPaymentTermDays: 10, maxPaymentTermDays: 30);

            Action act = () => policy.Update(maxDiscountPercent: null, defaultPaymentTermDays: 60, maxPaymentTermDays: 30, notes: null);

            act.Should().Throw<InvalidOperationException>().WithMessage("commercialPolicy.paymentTerm.defaultExceedsMax");
        }

        [Test]
        public void Constructor_should_allow_default_equal_or_below_max()
        {
            Action act = () => _ = new CommercialPolicy(maxDiscountPercent: null, defaultPaymentTermDays: 30, maxPaymentTermDays: 30);

            act.Should().NotThrow();
        }
    }
}
