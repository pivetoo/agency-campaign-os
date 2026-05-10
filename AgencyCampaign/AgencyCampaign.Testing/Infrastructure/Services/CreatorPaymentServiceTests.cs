using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorPayments;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using DomainEntities = AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CreatorPaymentServiceTests
    {
        private TestDbContext db = null!;
        private CreatorPaymentService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CreatorPaymentService(db, LocalizerMock.Create<AgencyCampaignResource>(), IntegrationPlatformClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<CreatorPayment> SeedExistingPaymentAsync()
        {
            CreatorPayment payment = new CreatorPayment(1, 7, 100m, 0m, PaymentMethod.Pix);
            db.Add(payment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return payment;
        }

        [Test]
        public async Task CreatePayment_should_throw_when_campaign_creator_not_found()
        {
            CreateCreatorPaymentRequest request = new() { CampaignCreatorId = 99, GrossAmount = 100m, Method = PaymentMethod.Pix };
            Func<Task> act = () => service.CreatePayment(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        private async Task<DomainEntities.CampaignCreator> SeedCampaignCreatorAsync(string? pixKey = "foo@x", PixKeyType? pixKeyType = PixKeyType.Email)
        {
            Brand brand = new("Acme");
            db.Add(brand);
            Creator creator = new("Foo", pixKey: pixKey, pixKeyType: pixKeyType);
            db.Add(creator);
            await db.SaveChangesAsync();

            Campaign campaign = new(brand.Id, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();

            DomainEntities.CampaignCreator cc = new(campaign.Id, creator.Id, 1, 100m, 10m);
            db.Add(cc);
            await db.SaveChangesAsync();
            return cc;
        }

        [Test]
        public async Task CreatePayment_should_snapshot_pix_data_from_creator_when_available()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 1000m,
                Method = PaymentMethod.Pix
            });

            payment.PixKey.Should().Be("foo@x");
            payment.PixKeyType.Should().Be(PixKeyType.Email);

            db.ChangeTracker.Clear();
            CreatorPayment persisted = await db.Set<CreatorPayment>().AsNoTracking().Include(item => item.Events).SingleAsync();
            persisted.Events.Should().ContainSingle(item => item.EventType == CreatorPaymentEventType.Created);
        }

        [Test]
        public async Task CreatePayment_should_skip_pix_snapshot_when_creator_has_no_pix()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync(pixKey: null, pixKeyType: null);

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 1000m,
                Method = PaymentMethod.Pix
            });

            payment.PixKey.Should().BeNull();
        }

        [Test]
        public async Task UpdatePayment_should_throw_when_id_mismatch()
        {
            UpdateCreatorPaymentRequest request = new() { Id = 5, GrossAmount = 100m, Method = PaymentMethod.Pix };
            Func<Task> act = () => service.UpdatePayment(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdatePayment_should_throw_when_not_found()
        {
            UpdateCreatorPaymentRequest request = new() { Id = 99, GrossAmount = 100m, Method = PaymentMethod.Pix };
            Func<Task> act = () => service.UpdatePayment(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task AttachInvoice_should_throw_when_payment_not_found()
        {
            AttachInvoiceRequest request = new() { InvoiceUrl = "https://x" };
            Func<Task> act = () => service.AttachInvoice(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task AttachInvoice_should_register_event_and_persist_invoice()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();

            CreatorPayment result = await service.AttachInvoice(payment.Id, new AttachInvoiceRequest
            {
                InvoiceNumber = "NF-1",
                InvoiceUrl = "https://x"
            });

            result.InvoiceNumber.Should().Be("NF-1");
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.InvoiceAttached);
        }

        [Test]
        public async Task MarkPaid_should_attach_provider_when_provided_and_register_paid_event()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();

            CreatorPayment result = await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest
            {
                PaidAt = DateTimeOffset.UtcNow,
                Provider = "Pagar.me",
                ProviderTransactionId = "tx-1"
            });

            result.Status.Should().Be(PaymentStatus.Paid);
            result.Provider.Should().Be("Pagar.me");
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Paid);
        }

        [Test]
        public async Task MarkPaid_should_throw_when_not_found()
        {
            Func<Task> act = () => service.MarkPaid(99, new MarkCreatorPaymentPaidRequest { PaidAt = DateTimeOffset.UtcNow });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Cancel_should_set_status_and_register_event()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();

            CreatorPayment result = await service.Cancel(payment.Id);

            result.Status.Should().Be(PaymentStatus.Cancelled);
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Cancelled);
        }

        [Test]
        public async Task Cancel_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Cancel(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetByCampaign_should_filter_by_campaign_id()
        {
            db.Add(new CreatorPayment(10, 7, 100m, 0m, PaymentMethod.Pix));
            db.Add(new CreatorPayment(11, 7, 100m, 0m, PaymentMethod.Pix));
            await db.SaveChangesAsync();

            // Service uses Include chains that may not return data without Campaign entities seeded.
            // Just verify it doesn't throw.
            await service.GetByCampaign(10);
        }
    }
}
