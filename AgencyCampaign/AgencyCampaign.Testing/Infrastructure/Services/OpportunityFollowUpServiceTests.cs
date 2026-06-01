using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityFollowUpServiceTests
    {
        private TestDbContext db = null!;
        private Mock<Archon.Application.Services.INotificationService> notifications = null!;
        private OpportunityFollowUpService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notifications = new();
            service = new OpportunityFollowUpService(db, notifications.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Opportunity> SeedOpportunityAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            await db.SaveChangesAsync();
            Opportunity opportunity = new(1, 1, "x", 0);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            return opportunity;
        }

        [Test]
        public async Task CreateOpportunityFollowUp_should_throw_when_opportunity_not_found()
        {
            CreateOpportunityFollowUpRequest request = new()
            {
                OpportunityId = 99,
                Subject = "call",
                DueAt = DateTimeOffset.UtcNow
            };

            Func<Task> act = () => service.CreateOpportunityFollowUp(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task RemindDue_should_notify_overdue_open_followups_once_and_skip_others()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            OpportunityFollowUp overdue = new(opportunity.Id, "Ligar para o cliente", DateTimeOffset.UtcNow.AddDays(-1));
            OpportunityFollowUp future = new(opportunity.Id, "Reuniao futura", DateTimeOffset.UtcNow.AddDays(5));
            OpportunityFollowUp completed = new(opportunity.Id, "Ja feito", DateTimeOffset.UtcNow.AddDays(-2));
            completed.Complete();
            db.Add(overdue);
            db.Add(future);
            db.Add(completed);
            await db.SaveChangesAsync();

            int count = await service.RemindDue();

            count.Should().Be(1);
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);

            db.ChangeTracker.Clear();
            (await db.Set<OpportunityFollowUp>().AsNoTracking().SingleAsync(item => item.Id == overdue.Id)).ReminderSentAt.Should().NotBeNull();

            (await service.RemindDue()).Should().Be(0);
        }

        [Test]
        public async Task CreateOpportunityFollowUp_should_persist()
        {
            Opportunity opportunity = await SeedOpportunityAsync();

            OpportunityFollowUp followUp = await service.CreateOpportunityFollowUp(new CreateOpportunityFollowUpRequest
            {
                OpportunityId = opportunity.Id,
                Subject = "call",
                DueAt = DateTimeOffset.UtcNow
            });

            followUp.Id.Should().BeGreaterThan(0);
            (await db.Set<OpportunityFollowUp>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task CompleteOpportunityFollowUp_should_mark_as_completed()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "call", DateTimeOffset.UtcNow);
            db.Add(followUp);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityFollowUp result = await service.CompleteOpportunityFollowUp(followUp.Id);

            result.IsCompleted.Should().BeTrue();
        }

        [Test]
        public async Task CompleteOpportunityFollowUp_should_throw_when_already_completed()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "call", DateTimeOffset.UtcNow);
            followUp.Complete();
            db.Add(followUp);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.CompleteOpportunityFollowUp(followUp.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ReopenOpportunityFollowUp_should_clear_completion()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "call", DateTimeOffset.UtcNow);
            followUp.Complete();
            db.Add(followUp);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityFollowUp result = await service.ReopenOpportunityFollowUp(followUp.Id);

            result.IsCompleted.Should().BeFalse();
            result.CompletedAt.Should().BeNull();
        }

        [Test]
        public async Task CompleteOpportunityFollowUp_should_throw_when_not_found()
        {
            Func<Task> act = () => service.CompleteOpportunityFollowUp(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetFollowUpsByOpportunityId_should_order_open_first_then_by_due_date()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            DateTimeOffset baseDate = DateTimeOffset.UtcNow;

            OpportunityFollowUp openFar = new(opportunity.Id, "open-far", baseDate.AddDays(10));
            OpportunityFollowUp openSoon = new(opportunity.Id, "open-soon", baseDate.AddDays(1));
            OpportunityFollowUp closed = new(opportunity.Id, "closed", baseDate.AddDays(-1));
            closed.Complete();

            db.Add(openFar);
            db.Add(openSoon);
            db.Add(closed);
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityFollowUp> result = await service.GetFollowUpsByOpportunityId(opportunity.Id);

            result.Select(item => item.Subject).Should().Equal("open-soon", "open-far", "closed");
        }

        [Test]
        public async Task GetOpportunityFollowUpById_should_return_null_when_not_found()
        {
            (await service.GetOpportunityFollowUpById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetOpportunityFollowUpById_should_return_follow_up_when_found()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "call", DateTimeOffset.UtcNow);
            db.Add(followUp);
            await db.SaveChangesAsync();

            OpportunityFollowUp? result = await service.GetOpportunityFollowUpById(followUp.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateOpportunityFollowUp_should_throw_when_not_found()
        {
            UpdateOpportunityFollowUpRequest request = new() { Subject = "x", DueAt = DateTimeOffset.UtcNow };

            Func<Task> act = () => service.UpdateOpportunityFollowUp(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateOpportunityFollowUp_should_persist_changes()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "old", DateTimeOffset.UtcNow);
            db.Add(followUp);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateOpportunityFollowUpRequest request = new()
            {
                Subject = "new subject",
                DueAt = DateTimeOffset.UtcNow.AddDays(1),
                Notes = "novas notas"
            };

            OpportunityFollowUp result = await service.UpdateOpportunityFollowUp(followUp.Id, request);

            result.Subject.Should().Be("new subject");
            result.Notes.Should().Be("novas notas");
        }

        [Test]
        public async Task DeleteOpportunityFollowUp_should_throw_when_not_found()
        {
            Func<Task> act = () => service.DeleteOpportunityFollowUp(99);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task DeleteOpportunityFollowUp_should_remove_record()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityFollowUp followUp = new(opportunity.Id, "call", DateTimeOffset.UtcNow);
            db.Add(followUp);
            await db.SaveChangesAsync();

            await service.DeleteOpportunityFollowUp(followUp.Id);

            (await db.Set<OpportunityFollowUp>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task GetAllFollowUps_should_return_all_when_no_status_filter()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new OpportunityFollowUp(opportunity.Id, "one", DateTimeOffset.UtcNow.AddDays(1)));
            db.Add(new OpportunityFollowUp(opportunity.Id, "two", DateTimeOffset.UtcNow.AddDays(2)));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityFollowUp> result = await service.GetAllFollowUps(status: null);

            result.Should().HaveCount(2);
        }

        [Test]
        public async Task GetAllFollowUps_should_filter_by_overdue_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new OpportunityFollowUp(opportunity.Id, "future", DateTimeOffset.UtcNow.AddDays(2)));
            db.Add(new OpportunityFollowUp(opportunity.Id, "past", DateTimeOffset.UtcNow.AddDays(-5)));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityFollowUp> result = await service.GetAllFollowUps("overdue");

            result.Select(item => item.Subject).Should().Contain("past");
        }

        [Test]
        public async Task GetAllFollowUps_should_filter_by_upcoming_status()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new OpportunityFollowUp(opportunity.Id, "upcoming", DateTimeOffset.UtcNow.AddDays(5)));
            db.Add(new OpportunityFollowUp(opportunity.Id, "past", DateTimeOffset.UtcNow.AddDays(-5)));
            await db.SaveChangesAsync();

            IReadOnlyCollection<OpportunityFollowUp> result = await service.GetAllFollowUps("upcoming");

            result.Select(item => item.Subject).Should().Contain("upcoming");
        }

        [Test]
        public async Task GetFollowUpsSummary_should_aggregate_counts()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            db.Add(new OpportunityFollowUp(opportunity.Id, "today", DateTimeOffset.UtcNow.AddHours(-1)));
            db.Add(new OpportunityFollowUp(opportunity.Id, "upcoming", DateTimeOffset.UtcNow.AddDays(5)));
            OpportunityFollowUp done = new(opportunity.Id, "done", DateTimeOffset.UtcNow.AddDays(-1));
            done.Complete();
            db.Add(done);
            await db.SaveChangesAsync();

            var result = await service.GetFollowUpsSummary();

            result.Completed.Should().Be(1);
            result.Upcoming.Should().Be(1);
        }
    }
}
