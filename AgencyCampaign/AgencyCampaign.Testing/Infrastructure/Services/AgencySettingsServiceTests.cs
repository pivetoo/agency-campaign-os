using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models;
using AgencyCampaign.Application.Requests.AgencySettings;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class AgencySettingsServiceTests
    {
        private TestDbContext db = null!;
        private AgencySettingsService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new AgencySettingsService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Get_should_create_default_settings_when_missing()
        {
            AgencySettingsModel result = await service.Get();

            result.Id.Should().BeGreaterThan(0);
            result.AgencyName.Should().Be("Minha agência");
            (await db.Set<AgencySettings>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Get_should_return_existing_settings_without_duplicating()
        {
            await service.Get();
            await service.Get();

            (await db.Set<AgencySettings>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Update_should_replace_state_creating_record_if_missing()
        {
            AgencySettingsModel result = await service.Update(new UpdateAgencySettingsRequest
            {
                AgencyName = "Acme Agency",
                TradeName = "Acme",
                PrimaryEmail = "agency@x",
                PrimaryColor = "#fff"
            });

            result.AgencyName.Should().Be("Acme Agency");
            result.TradeName.Should().Be("Acme");
            (await db.Set<AgencySettings>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task SetLogo_should_persist_url()
        {
            AgencySettingsModel result = await service.SetLogo("/uploads/agency/logo.png");

            result.LogoUrl.Should().Be("/uploads/agency/logo.png");
        }

        [Test]
        public async Task RemoveLogo_should_clear_url()
        {
            await service.SetLogo("/uploads/agency/logo.png");

            AgencySettingsModel result = await service.RemoveLogo();

            result.LogoUrl.Should().BeNull();
        }

        [Test]
        public async Task SaveProposalTemplate_should_persist_template()
        {
            AgencySettingsModel result = await service.SaveProposalTemplate("<p>{{name}}</p>");

            result.ProposalHtmlTemplate.Should().Be("<p>{{name}}</p>");
        }

        [Test]
        public async Task PreviewProposalTemplate_should_render_template_string()
        {
            string preview = await service.PreviewProposalTemplate("<p>Preview</p>");

            preview.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task GetProposalLayouts_should_return_builtin_layouts()
        {
            IReadOnlyList<ProposalLayoutModel> result = await service.GetProposalLayouts();

            result.Should().NotBeEmpty();
        }

        [Test]
        public async Task GetProposalTemplateVersions_should_return_empty_when_none()
        {
            IReadOnlyList<ProposalTemplateVersionModel> result = await service.GetProposalTemplateVersions();

            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetProposalTemplateVersions_should_return_ordered_desc_by_created_at()
        {
            await service.SaveProposalTemplateVersion("v1", "<p>1</p>", activate: false);
            await Task.Delay(10);
            await service.SaveProposalTemplateVersion("v2", "<p>2</p>", activate: false);

            IReadOnlyList<ProposalTemplateVersionModel> result = await service.GetProposalTemplateVersions();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("v2");
        }

        [Test]
        public async Task SaveProposalTemplateVersion_without_activate_should_keep_settings_template_untouched()
        {
            ProposalTemplateVersionModel result = await service.SaveProposalTemplateVersion("v1", "<p>x</p>", activate: false);

            result.IsActive.Should().BeFalse();
            AgencySettings? settings = await db.Set<AgencySettings>().AsNoTracking().FirstOrDefaultAsync();
            settings?.ProposalHtmlTemplate.Should().BeNull();
        }

        [Test]
        public async Task SaveProposalTemplateVersion_with_activate_should_activate_and_apply()
        {
            ProposalTemplateVersionModel result = await service.SaveProposalTemplateVersion("v1", "<p>active</p>", activate: true);

            result.IsActive.Should().BeTrue();
            AgencySettings settings = await db.Set<AgencySettings>().AsNoTracking().FirstAsync();
            settings.ProposalHtmlTemplate.Should().Be("<p>active</p>");
        }

        [Test]
        public async Task SaveProposalTemplateVersion_with_activate_should_deactivate_others()
        {
            await service.SaveProposalTemplateVersion("v1", "<p>1</p>", activate: true);
            await service.SaveProposalTemplateVersion("v2", "<p>2</p>", activate: true);

            List<ProposalTemplateVersion> all = await db.Set<ProposalTemplateVersion>().AsNoTracking().ToListAsync();
            all.Should().HaveCount(2);
            all.Count(v => v.IsActive).Should().Be(1);
            all.First(v => v.IsActive).Name.Should().Be("v2");
        }

        [Test]
        public async Task ActivateProposalTemplateVersion_should_throw_when_not_found()
        {
            Func<Task> act = () => service.ActivateProposalTemplateVersion(99);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task ActivateProposalTemplateVersion_should_activate_and_apply_template()
        {
            ProposalTemplateVersionModel created = await service.SaveProposalTemplateVersion("v1", "<p>new</p>", activate: false);

            ProposalTemplateVersionModel result = await service.ActivateProposalTemplateVersion(created.Id);

            result.IsActive.Should().BeTrue();
            AgencySettings settings = await db.Set<AgencySettings>().AsNoTracking().FirstAsync();
            settings.ProposalHtmlTemplate.Should().Be("<p>new</p>");
        }

        [Test]
        public async Task DeleteProposalTemplateVersion_should_remove_when_exists()
        {
            ProposalTemplateVersionModel created = await service.SaveProposalTemplateVersion("v1", "<p>x</p>", activate: false);

            await service.DeleteProposalTemplateVersion(created.Id);

            (await db.Set<ProposalTemplateVersion>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task DeleteProposalTemplateVersion_should_noop_when_not_found()
        {
            await service.DeleteProposalTemplateVersion(99);

            (await db.Set<ProposalTemplateVersion>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task GetProposalTemplateVersionById_should_return_null_when_not_found()
        {
            ProposalTemplateVersionModel? result = await service.GetProposalTemplateVersionById(99);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetProposalTemplateVersionById_should_return_version_when_found()
        {
            ProposalTemplateVersionModel created = await service.SaveProposalTemplateVersion("v1", "<p>x</p>", activate: false);

            ProposalTemplateVersionModel? result = await service.GetProposalTemplateVersionById(created.Id);

            result.Should().NotBeNull();
            result!.Name.Should().Be("v1");
        }

        [Test]
        public async Task UpdateProposalTemplateVersion_should_throw_when_not_found()
        {
            Func<Task> act = () => service.UpdateProposalTemplateVersion(99, "x", "<p>x</p>", isDefault: false);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task UpdateProposalTemplateVersion_with_isDefault_should_activate_and_apply()
        {
            ProposalTemplateVersionModel created = await service.SaveProposalTemplateVersion("v1", "<p>old</p>", activate: false);

            ProposalTemplateVersionModel result = await service.UpdateProposalTemplateVersion(created.Id, "v1-edited", "<p>edited</p>", isDefault: true);

            result.IsActive.Should().BeTrue();
            result.Name.Should().Be("v1-edited");
            AgencySettings settings = await db.Set<AgencySettings>().AsNoTracking().FirstAsync();
            settings.ProposalHtmlTemplate.Should().Be("<p>edited</p>");
        }

        [Test]
        public async Task UpdateProposalTemplateVersion_without_isDefault_should_deactivate_when_active()
        {
            ProposalTemplateVersionModel created = await service.SaveProposalTemplateVersion("v1", "<p>x</p>", activate: true);

            ProposalTemplateVersionModel result = await service.UpdateProposalTemplateVersion(created.Id, "v1", "<p>edited</p>", isDefault: false);

            result.IsActive.Should().BeFalse();
        }
    }
}
