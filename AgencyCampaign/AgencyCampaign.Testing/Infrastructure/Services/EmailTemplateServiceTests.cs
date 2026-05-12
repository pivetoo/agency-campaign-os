using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.EmailTemplates;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class EmailTemplateServiceTests
    {
        private TestDbContext db = null!;
        private EmailTemplateService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new EmailTemplateService(db, CurrentUserMock.Create(userId: 5, userName: "Tester"), LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_persist_with_current_user_data()
        {
            CreateEmailTemplateRequest request = new()
            {
                Name = "Boas vindas",
                EventType = EmailEventType.ProposalSent,
                Subject = "Olá",
                HtmlBody = "<b>Hi {{name}}</b>"
            };

            EmailTemplateModel result = await service.Create(request);

            result.Id.Should().BeGreaterThan(0);
            result.CreatedByUserName.Should().Be("Tester");
            (await db.Set<EmailTemplate>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateEmailTemplateRequest request = new() { Id = 5, Name = "x", Subject = "x", HtmlBody = "x" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_throw_when_not_found()
        {
            UpdateEmailTemplateRequest request = new() { Id = 1, Name = "x", Subject = "x", HtmlBody = "x" };
            Func<Task> act = () => service.Update(1, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_replace_state()
        {
            EmailTemplate template = new("Old", EmailEventType.ProposalSent, "Old subject", "<p>old</p>", null, null);
            db.Add(template);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            EmailTemplateModel result = await service.Update(template.Id, new UpdateEmailTemplateRequest
            {
                Id = template.Id,
                Name = "New",
                EventType = EmailEventType.ProposalApproved,
                Subject = "New subject",
                HtmlBody = "<p>new</p>",
                IsActive = false
            });

            result.Name.Should().Be("New");
            result.EventType.Should().Be(EmailEventType.ProposalApproved);
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task Delete_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Delete(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Delete_should_remove_template()
        {
            EmailTemplate template = new("X", EmailEventType.ProposalSent, "x", "y", null, null);
            db.Add(template);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await service.Delete(template.Id);

            (await db.Set<EmailTemplate>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task GetAll_should_filter_inactive_when_requested()
        {
            EmailTemplate active = new("Active", EmailEventType.ProposalSent, "x", "y", null, null);
            EmailTemplate inactive = new("Inactive", EmailEventType.ProposalSent, "x", "y", null, null);
            inactive.Update("Inactive", EmailEventType.ProposalSent, "x", "y", isActive: false);

            db.Add(active);
            db.Add(inactive);
            await db.SaveChangesAsync();

            IReadOnlyCollection<EmailTemplateModel> withInactive = await service.GetAll(includeInactive: true);
            IReadOnlyCollection<EmailTemplateModel> activeOnly = await service.GetAll(includeInactive: false);

            withInactive.Should().HaveCount(2);
            activeOnly.Should().ContainSingle(item => item.Name == "Active");
        }

        [Test]
        public async Task GetById_should_return_null_when_not_found()
        {
            (await service.GetById(99)).Should().BeNull();
        }
    }

    [TestFixture]
    public sealed class EmailServiceTests
    {
        private TestDbContext db = null!;
        private EmailService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new EmailService(db, IntegrationPlatformClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task SendForEvent_should_throw_on_null_recipients()
        {
            Func<Task> act = () => service.SendForEvent(EmailEventType.ProposalSent, null!, new Dictionary<string, object?>());
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task SendForEvent_should_throw_on_null_payload()
        {
            Func<Task> act = () => service.SendForEvent(EmailEventType.ProposalSent, new[] { "x@y" }, null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task SendForEvent_should_be_no_op_for_empty_recipients()
        {
            Func<Task> act = () => service.SendForEvent(EmailEventType.ProposalSent, Array.Empty<string>(), new Dictionary<string, object?>());
            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task SendForEvent_should_be_no_op_when_no_template_active_for_event()
        {
            Func<Task> act = () => service.SendForEvent(EmailEventType.ProposalSent, new[] { "x@y" }, new Dictionary<string, object?>());
            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task SendForEvent_should_resolve_active_template_for_event()
        {
            EmailTemplate template = new("T", EmailEventType.ProposalSent, "Hello {{name}}", "<p>Hi {{name}}</p>", null, null);
            db.Add(template);
            await db.SaveChangesAsync();

            // No exception means rendering succeeded; full network send is not implemented.
            await service.SendForEvent(EmailEventType.ProposalSent, new[] { "x@y" }, new Dictionary<string, object?> { ["name"] = "Foo" });
        }
    }
}
