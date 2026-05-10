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
        public async Task CreateDocument_should_throw_when_campaign_not_found()
        {
            CreateCampaignDocumentRequest request = new()
            {
                CampaignId = 99,
                DocumentType = CampaignDocumentType.CreatorAgreement,
                Title = "Contrato"
            };

            Func<Task> act = () => service.CreateDocument(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateDocument_should_persist_with_ready_to_send_status_and_creation_events()
        {
            db.Add(new Brand("Acme"));
            await db.SaveChangesAsync();
            Campaign campaign = new(1, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();

            CampaignDocument result = await service.CreateDocument(new CreateCampaignDocumentRequest
            {
                CampaignId = campaign.Id,
                DocumentType = CampaignDocumentType.CreatorAgreement,
                Title = "Contrato"
            });

            result.Status.Should().Be(CampaignDocumentStatus.ReadyToSend);

            db.ChangeTracker.Clear();
            CampaignDocument persisted = await db.Set<CampaignDocument>()
                .AsNoTracking()
                .Include(item => item.Events)
                .SingleAsync();

            persisted.Events.Should().Contain(item => item.EventType == CampaignDocumentEventType.Created);
            persisted.Events.Should().Contain(item => item.EventType == CampaignDocumentEventType.ReadyToSend);
        }
    }
}
