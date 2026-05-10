using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocumentTemplates;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignDocumentTemplateServiceTests
    {
        private TestDbContext db = null!;
        private CampaignDocumentTemplateService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CampaignDocumentTemplateService(db, LocalizerMock.Create<AgencyCampaignResource>(), CurrentUserMock.Create());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreateTemplate_should_persist()
        {
            CampaignDocumentTemplate result = await service.CreateTemplate(new CreateCampaignDocumentTemplateRequest
            {
                Name = "Padrão",
                DocumentType = CampaignDocumentType.CreatorAgreement,
                Body = "Corpo do contrato com mais de 10 caracteres"
            });

            result.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task UpdateTemplate_should_throw_when_id_mismatch()
        {
            UpdateCampaignDocumentTemplateRequest request = new() { Id = 5, Name = "x", Body = "1234567890", DocumentType = CampaignDocumentType.CreatorAgreement };
            Func<Task> act = () => service.UpdateTemplate(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateTemplate_should_throw_when_not_found()
        {
            UpdateCampaignDocumentTemplateRequest request = new() { Id = 99, Name = "x", Body = "1234567890", DocumentType = CampaignDocumentType.CreatorAgreement };
            Func<Task> act = () => service.UpdateTemplate(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetActiveByDocumentType_should_filter_by_type_and_active()
        {
            db.Add(new CampaignDocumentTemplate("Active CA", CampaignDocumentType.CreatorAgreement, "body".PadRight(20), null, null, null));
            CampaignDocumentTemplate inactiveSameType = new("Inactive CA", CampaignDocumentType.CreatorAgreement, "body".PadRight(20), null, null, null);
            inactiveSameType.Deactivate();
            db.Add(inactiveSameType);
            db.Add(new CampaignDocumentTemplate("Active BC", CampaignDocumentType.BrandContract, "body".PadRight(20), null, null, null));
            await db.SaveChangesAsync();

            List<CampaignDocumentTemplate> result = await service.GetActiveByDocumentType(CampaignDocumentType.CreatorAgreement);
            result.Should().ContainSingle(item => item.Name == "Active CA");
        }

        [Test]
        public async Task DeleteTemplate_should_deactivate_when_in_use_and_return_false()
        {
            CampaignDocumentTemplate template = new("T", CampaignDocumentType.CreatorAgreement, "body".PadRight(20), null, null, null);
            db.Add(template);
            await db.SaveChangesAsync();

            CampaignDocument doc = new(campaignId: 1, documentType: CampaignDocumentType.CreatorAgreement, title: "Doc", templateId: template.Id);
            db.Add(doc);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            bool result = await service.DeleteTemplate(template.Id);

            result.Should().BeFalse();
            CampaignDocumentTemplate persisted = await db.Set<CampaignDocumentTemplate>().AsNoTracking().FirstAsync();
            persisted.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task DeleteTemplate_should_throw_when_not_found()
        {
            Func<Task> act = () => service.DeleteTemplate(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
