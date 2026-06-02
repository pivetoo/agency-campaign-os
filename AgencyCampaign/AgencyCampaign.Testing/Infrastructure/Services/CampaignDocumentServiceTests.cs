using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;

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
            service = new CampaignDocumentService(db, IntegrationPlatformClientFactory.CreateInert(), new IntegrationCapabilityService(db));
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

        private async Task<Campaign> SeedCampaignAsync()
        {
            db.Add(new Brand("Acme"));
            await db.SaveChangesAsync();
            Campaign campaign = new(1, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();
            return campaign;
        }

        [Test]
        public async Task GetDocumentById_should_return_null_when_not_found()
        {
            CampaignDocument? result = await service.GetDocumentById(99);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetByProviderDocumentId_should_return_null_when_not_found()
        {
            CampaignDocument? result = await service.GetByProviderDocumentId("provider", "doc-123");

            result.Should().BeNull();
        }

        [Test]
        public async Task GetByCampaign_should_filter_by_campaign_id()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocument doc1 = new(campaign.Id, CampaignDocumentType.CreatorAgreement, "A");
            CampaignDocument doc2 = new(999, CampaignDocumentType.CreatorAgreement, "B");
            db.Add(doc1);
            db.Add(doc2);
            await db.SaveChangesAsync();

            List<CampaignDocument> result = await service.GetByCampaign(campaign.Id);

            result.Should().HaveCount(1);
            result.First().Title.Should().Be("A");
        }

        [Test]
        public async Task CreateDocument_should_throw_when_campaign_creator_does_not_belong_to_campaign()
        {
            Campaign campaign = await SeedCampaignAsync();
            CreateCampaignDocumentRequest request = new()
            {
                CampaignId = campaign.Id,
                CampaignCreatorId = 999,
                DocumentType = CampaignDocumentType.CreatorAgreement,
                Title = "Contrato"
            };

            Func<Task> act = () => service.CreateDocument(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task GenerateFromTemplate_should_throw_when_template_not_found()
        {
            Campaign campaign = await SeedCampaignAsync();
            GenerateCampaignDocumentFromTemplateRequest request = new()
            {
                CampaignId = campaign.Id,
                TemplateId = 99,
                Title = "Doc"
            };

            Func<Task> act = () => service.GenerateFromTemplate(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task GenerateFromTemplate_should_reject_unknown_template_variables()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocumentTemplate template = new("Contrato", CampaignDocumentType.CreatorAgreement, "Ola {{creatorNam}}, campanha {{campaignName}}.");
            db.Add(template);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.GenerateFromTemplate(new GenerateCampaignDocumentFromTemplateRequest
            {
                CampaignId = campaign.Id,
                TemplateId = template.Id,
                Title = "Doc"
            });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*creatorNam*");
        }

        [Test]
        public async Task GenerateFromTemplate_should_render_known_variables()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocumentTemplate template = new("Contrato", CampaignDocumentType.CreatorAgreement, "Campanha {{campaignName}} em {{today}}.");
            db.Add(template);
            await db.SaveChangesAsync();

            CampaignDocument document = await service.GenerateFromTemplate(new GenerateCampaignDocumentFromTemplateRequest
            {
                CampaignId = campaign.Id,
                TemplateId = template.Id,
                Title = "Doc"
            });

            document.Body.Should().Contain("Campanha C em");
            document.Body.Should().NotContain("{{");
        }

        [Test]
        public async Task GenerateFromTemplate_should_accept_variables_supplied_via_overrides()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocumentTemplate template = new("Contrato", CampaignDocumentType.CreatorAgreement, "Clausula: {{customClause}}.");
            db.Add(template);
            await db.SaveChangesAsync();

            CampaignDocument document = await service.GenerateFromTemplate(new GenerateCampaignDocumentFromTemplateRequest
            {
                CampaignId = campaign.Id,
                TemplateId = template.Id,
                Title = "Doc",
                Overrides = new Dictionary<string, string> { ["customClause"] = "Exclusividade de 30 dias" }
            });

            document.Body.Should().Contain("Exclusividade de 30 dias");
        }

        [Test]
        public async Task UpdateDocument_should_persist_changes()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocument doc = new(campaign.Id, CampaignDocumentType.CreatorAgreement, "Original");
            db.Add(doc);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignDocumentRequest request = new()
            {
                Id = doc.Id,
                DocumentType = CampaignDocumentType.CreatorAgreement,
                Title = "Atualizado",
                Notes = "Novas notas"
            };

            CampaignDocument result = await service.UpdateDocument(doc.Id, request);

            result.Title.Should().Be("Atualizado");
            result.Notes.Should().Be("Novas notas");
        }

        [Test]
        public async Task HandleProviderCallback_should_throw_when_document_not_found()
        {
            CampaignDocumentProviderCallbackRequest request = new()
            {
                Provider = "p",
                ProviderDocumentId = "doc-x",
                EventType = "signed"
            };

            Func<Task> act = () => service.HandleProviderCallback(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task HandleProviderCallback_should_register_viewed_event()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocument doc = new(campaign.Id, CampaignDocumentType.CreatorAgreement, "Doc");
            doc.AttachToProvider("provider-x", "doc-1");
            db.Add(doc);
            await db.SaveChangesAsync();

            CampaignDocument result = await service.HandleProviderCallback(new CampaignDocumentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderDocumentId = "doc-1",
                EventType = "viewed"
            });

            result.Events.Should().Contain(item => item.EventType == CampaignDocumentEventType.Viewed);
        }

        [Test]
        public async Task HandleProviderCallback_should_mark_signed_on_completed_event()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocument doc = new(campaign.Id, CampaignDocumentType.CreatorAgreement, "Doc");
            doc.AttachToProvider("provider-x", "doc-2");
            db.Add(doc);
            await db.SaveChangesAsync();

            CampaignDocument result = await service.HandleProviderCallback(new CampaignDocumentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderDocumentId = "doc-2",
                EventType = "completed",
                SignedDocumentUrl = "/signed.pdf"
            });

            result.Status.Should().Be(CampaignDocumentStatus.Signed);
        }

        [Test]
        public async Task HandleProviderCallback_should_mark_cancelled_on_cancelled_event()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocument doc = new(campaign.Id, CampaignDocumentType.CreatorAgreement, "Doc");
            doc.AttachToProvider("provider-x", "doc-3");
            db.Add(doc);
            await db.SaveChangesAsync();

            CampaignDocument result = await service.HandleProviderCallback(new CampaignDocumentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderDocumentId = "doc-3",
                EventType = "cancelled"
            });

            result.Status.Should().Be(CampaignDocumentStatus.Cancelled);
        }

        [Test]
        public async Task HandleProviderCallback_should_register_unknown_event_as_sync_error()
        {
            Campaign campaign = await SeedCampaignAsync();
            CampaignDocument doc = new(campaign.Id, CampaignDocumentType.CreatorAgreement, "Doc");
            doc.AttachToProvider("provider-x", "doc-4");
            db.Add(doc);
            await db.SaveChangesAsync();

            CampaignDocument result = await service.HandleProviderCallback(new CampaignDocumentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderDocumentId = "doc-4",
                EventType = "estranho"
            });

            result.Events.Should().Contain(item => item.EventType == CampaignDocumentEventType.ProviderSyncError);
        }
    }
}
