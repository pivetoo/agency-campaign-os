using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorPortal;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CreatorPortalServiceTests
    {
        private TestDbContext db = null!;
        private Mock<ICreatorAccessTokenService> tokenService = null!;
        private CreatorPortalService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            tokenService = new Mock<ICreatorAccessTokenService>();
            service = new CreatorPortalService(db, tokenService.Object, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task ResolveContext_should_throw_when_token_invalid()
        {
            tokenService
                .Setup(item => item.ValidateToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreatorAccessToken?)null);

            Func<Task> act = () => service.ResolveContext("bad");
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ResolveContext_should_return_context_with_creator()
        {
            Creator creator = new Creator("Foo").WithId(1);
            CreatorAccessToken token = new(1, "tok");
            // Set Creator nav property via reflection
            typeof(CreatorAccessToken).GetProperty(nameof(CreatorAccessToken.Creator))!.SetValue(token, creator);
            tokenService
                .Setup(item => item.ValidateToken("tok", It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            CreatorPortalContext context = await service.ResolveContext("tok");

            context.Creator.Should().BeSameAs(creator);
            context.Token.Should().BeSameAs(token);
        }

        [Test]
        public async Task UpdateBankInfo_should_throw_when_creator_not_found()
        {
            Func<Task> act = () => service.UpdateBankInfo(99, new UpdateCreatorBankInfoRequest { PixKey = "pix", PixKeyType = PixKeyType.Email });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateBankInfo_should_persist_pix_data()
        {
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Creator result = await service.UpdateBankInfo(creator.Id, new UpdateCreatorBankInfoRequest
            {
                PixKey = "foo@x",
                PixKeyType = PixKeyType.Email,
                Document = "12345"
            });

            result.PixKey.Should().Be("foo@x");
            result.PixKeyType.Should().Be(PixKeyType.Email);
            result.Document.Should().Be("12345");
        }

        [Test]
        public async Task UploadInvoice_should_throw_when_payment_not_found_or_not_owned()
        {
            CreatorPayment payment = new(1, 7, 100m, 0m, PaymentMethod.Pix);
            db.Add(payment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.UploadInvoice(creatorId: 99, new UploadInvoiceRequest
            {
                CreatorPaymentId = payment.Id,
                InvoiceUrl = "https://x"
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UploadInvoice_should_attach_invoice_and_register_event()
        {
            CreatorPayment payment = new CreatorPayment(1, 7, 100m, 0m, PaymentMethod.Pix).WithId(50);
            db.Add(payment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreatorPayment result = await service.UploadInvoice(creatorId: 7, new UploadInvoiceRequest
            {
                CreatorPaymentId = payment.Id,
                InvoiceNumber = "NF-1",
                InvoiceUrl = "https://x",
                IssuedAt = DateTimeOffset.UtcNow
            });

            result.InvoiceNumber.Should().Be("NF-1");
            result.InvoiceUrl.Should().Be("https://x");
            result.Events.Should().ContainSingle(item => item.EventType == CreatorPaymentEventType.InvoiceAttached);
        }

    }
}
