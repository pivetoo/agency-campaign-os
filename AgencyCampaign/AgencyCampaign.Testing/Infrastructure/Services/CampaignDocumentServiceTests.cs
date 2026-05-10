using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignDocumentServiceTests
    {
        private TestDbContext db = null!;
        private CampaignDocumentService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CampaignDocumentService(db,
                LocalizerMock.Create<AgencyCampaignResource>(),
                Options.Create(new DocumentEmailOptions()),
                IntegrationPlatformClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task GetByProviderDocumentId_should_reject_blank_inputs()
        {
            Func<Task> blankProvider = () => service.GetByProviderDocumentId(" ", "id");
            Func<Task> blankId = () => service.GetByProviderDocumentId("p", " ");
            await blankProvider.Should().ThrowAsync<ArgumentException>();
            await blankId.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public async Task UpdateDocument_should_throw_when_id_mismatch()
        {
            UpdateCampaignDocumentRequest request = new() { Id = 5, Title = "x", DocumentType = CampaignDocumentType.CreatorAgreement };
            Func<Task> act = () => service.UpdateDocument(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateDocument_should_throw_when_not_found()
        {
            UpdateCampaignDocumentRequest request = new() { Id = 99, Title = "x", DocumentType = CampaignDocumentType.CreatorAgreement };
            Func<Task> act = () => service.UpdateDocument(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task MarkAsSigned_should_throw_when_not_found()
        {
            Func<Task> act = () => service.MarkAsSigned(99, new MarkCampaignDocumentSignedRequest { SignedAt = DateTimeOffset.UtcNow });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task MarkAsSigned_should_set_status_signed()
        {
            CampaignDocument doc = new(campaignId: 1, documentType: CampaignDocumentType.CreatorAgreement, title: "Doc");
            db.Add(doc);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CampaignDocument result = await service.MarkAsSigned(doc.Id, new MarkCampaignDocumentSignedRequest { SignedAt = DateTimeOffset.UtcNow });

            result.Status.Should().Be(CampaignDocumentStatus.Signed);
            result.Events.Should().Contain(item => item.EventType == CampaignDocumentEventType.Signed);
        }

        [Test]
        public async Task GetByCampaign_should_filter_by_campaign_id()
        {
            db.Add(new CampaignDocument(1, CampaignDocumentType.CreatorAgreement, "A"));
            db.Add(new CampaignDocument(1, CampaignDocumentType.BrandContract, "B"));
            db.Add(new CampaignDocument(99, CampaignDocumentType.CreatorAgreement, "Outro"));
            await db.SaveChangesAsync();

            List<CampaignDocument> result = await service.GetByCampaign(1);
            result.Should().HaveCount(2);
        }
    }
}
