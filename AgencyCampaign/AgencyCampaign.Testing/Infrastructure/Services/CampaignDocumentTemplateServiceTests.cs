using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocumentTemplates;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
            service = new CampaignDocumentTemplateService(db, CurrentUserMock.Create());
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

        [Test]
        public async Task GetTemplates_should_return_paged_result()
        {
            db.Add(new CampaignDocumentTemplate("A", CampaignDocumentType.CreatorAgreement, "body".PadRight(20), null, null, null));
            db.Add(new CampaignDocumentTemplate("B", CampaignDocumentType.BrandContract, "body".PadRight(20), null, null, null));
            await db.SaveChangesAsync();

            Archon.Core.Pagination.PagedResult<CampaignDocumentTemplate> result = await service.GetTemplates(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task GetTemplateById_should_return_null_when_not_found()
        {
            (await service.GetTemplateById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetTemplateById_should_return_template_when_found()
        {
            CampaignDocumentTemplate template = new("X", CampaignDocumentType.CreatorAgreement, "body".PadRight(20), null, null, null);
            db.Add(template);
            await db.SaveChangesAsync();

            CampaignDocumentTemplate? result = await service.GetTemplateById(template.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateTemplate_should_persist_changes()
        {
            CampaignDocumentTemplate template = new("Old", CampaignDocumentType.CreatorAgreement, "old body padded".PadRight(20), null, null, null);
            db.Add(template);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateCampaignDocumentTemplateRequest request = new()
            {
                Id = template.Id,
                Name = "Updated",
                DocumentType = CampaignDocumentType.BrandContract,
                Body = "new body that is long enough"
            };

            CampaignDocumentTemplate result = await service.UpdateTemplate(template.Id, request);

            result.Name.Should().Be("Updated");
            result.DocumentType.Should().Be(CampaignDocumentType.BrandContract);
        }

        [Test]
        public async Task DeleteTemplate_when_not_in_use_should_not_throw()
        {
            CampaignDocumentTemplate template = new("X", CampaignDocumentType.CreatorAgreement, "body".PadRight(20), null, null, null);
            db.Add(template);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.DeleteTemplate(template.Id);

            await act.Should().NotThrowAsync();
        }
    }
}
