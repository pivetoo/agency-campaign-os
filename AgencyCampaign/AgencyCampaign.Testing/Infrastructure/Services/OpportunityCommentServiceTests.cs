using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Abstractions;
using Archon.Application.Services;
using Archon.Core.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityCommentServiceTests
    {
        private TestDbContext db = null!;
        private Mock<INotificationService> notification = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            notification = new Mock<INotificationService>();
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private OpportunityCommentService BuildService(ICurrentUser currentUser)
        {
            return new OpportunityCommentService(db, currentUser, notification.Object);
        }

        private async Task<Opportunity> SeedOpportunityAsync()
        {
            db.Add(new CommercialPipelineStageBuilder().WithId(1).Build());
            await db.SaveChangesAsync();
            Opportunity opportunity = new(1, 1, "x", 0);
            db.Add(opportunity);
            await db.SaveChangesAsync();
            return opportunity;
        }

        [Test]
        public async Task CreateComment_should_throw_when_opportunity_not_found()
        {
            OpportunityCommentService service = BuildService(CurrentUserMock.Create());

            Func<Task> act = () => service.CreateComment(99, new CreateOpportunityCommentRequest { Body = "x" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateComment_should_use_username_as_author_name()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7, userName: "Alice"));

            OpportunityCommentModel result = await service.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest { Body = "  hello  " });

            result.AuthorName.Should().Be("Alice");
            result.AuthorUserId.Should().Be(7);
            result.Body.Should().Be("hello");
        }

        [Test]
        public async Task CreateComment_should_fall_back_to_email_then_sistema()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService viaEmail = BuildService(CurrentUserMock.Create(userId: 7, userName: null, email: "alice@x"));

            OpportunityCommentModel byEmail = await viaEmail.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest { Body = "x" });
            byEmail.AuthorName.Should().Be("alice@x");

            OpportunityCommentService viaSystem = BuildService(CurrentUserMock.Create(userId: null, userName: null, email: null));
            OpportunityCommentModel bySystem = await viaSystem.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest { Body = "y" });
            bySystem.AuthorName.Should().Be("Sistema");
        }

        [Test]
        public async Task UpdateComment_should_throw_when_user_is_not_author()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityComment comment = new(opportunity.Id, "body", authorUserId: 7, authorName: "Alice");
            db.Add(comment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 99));

            Func<Task> act = () => service.UpdateComment(comment.Id, new UpdateOpportunityCommentRequest { Body = "edited" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateComment_should_persist_when_user_is_author()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityComment comment = new(opportunity.Id, "body", authorUserId: 7, authorName: "Alice");
            db.Add(comment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7));
            OpportunityCommentModel result = await service.UpdateComment(comment.Id, new UpdateOpportunityCommentRequest { Body = "  edited  " });

            result.Body.Should().Be("edited");
        }

        [Test]
        public async Task DeleteComment_should_throw_when_user_is_not_author()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityComment comment = new(opportunity.Id, "body", authorUserId: 7, authorName: "Alice");
            db.Add(comment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 99));

            Func<Task> act = () => service.DeleteComment(comment.Id);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task DeleteComment_should_soft_delete_when_user_is_author()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityComment comment = new(opportunity.Id, "body", authorUserId: 7, authorName: "Alice");
            db.Add(comment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7));
            await service.DeleteComment(comment.Id);

            OpportunityComment persisted = await db.Set<OpportunityComment>().AsNoTracking().FirstAsync();
            persisted.IsDeleted.Should().BeTrue();
            persisted.Body.Should().Be("body");
        }

        [Test]
        public async Task GetByOpportunityId_should_throw_when_opportunity_not_found()
        {
            OpportunityCommentService service = BuildService(CurrentUserMock.Create());
            Func<Task> act = () => service.GetByOpportunityId(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetByOpportunityId_should_return_comments_in_descending_order_and_blank_body_for_deleted()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityComment first = new(opportunity.Id, "primeiro", authorUserId: 1, authorName: "A");
            OpportunityComment second = new(opportunity.Id, "segundo", authorUserId: 1, authorName: "A");
            second.MarkAsDeleted();
            db.Add(first);
            await db.SaveChangesAsync();
            await Task.Delay(5);
            db.Add(second);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityCommentService service = BuildService(CurrentUserMock.Create());

            IReadOnlyCollection<OpportunityCommentModel> result = await service.GetByOpportunityId(opportunity.Id);

            result.Should().HaveCount(2);
            OpportunityCommentModel deletedModel = result.First(item => item.IsDeleted);
            deletedModel.Body.Should().Be(string.Empty);
        }

        [Test]
        public async Task GetByOpportunityId_should_return_empty_when_no_comments_for_opportunity()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService service = BuildService(CurrentUserMock.Create());

            IReadOnlyCollection<OpportunityCommentModel> result = await service.GetByOpportunityId(opportunity.Id);

            result.Should().BeEmpty();
        }

        [Test]
        public async Task CreateComment_should_notify_distinct_mentioned_users_skipping_self()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7, userName: "Alice"));

            await service.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest
            {
                Body = "Olá pessoal",
                MentionedUserIds = new List<long> { 10, 20, 20, 7, 0, -1 }
            });

            notification.Verify(item => item.Create(It.Is<CreateNotificationRequest>(req =>
                req.UserId == 10 && req.Source == "AgencyCampaign.OpportunityComment"), It.IsAny<CancellationToken>()), Times.Once);
            notification.Verify(item => item.Create(It.Is<CreateNotificationRequest>(req => req.UserId == 20), It.IsAny<CancellationToken>()), Times.Once);
            notification.Verify(item => item.Create(It.Is<CreateNotificationRequest>(req => req.UserId == 7), It.IsAny<CancellationToken>()), Times.Never);
            notification.Verify(item => item.Create(It.IsAny<CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task CreateComment_should_not_notify_when_no_mentions_provided()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7, userName: "Alice"));

            await service.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest { Body = "sem mention" });

            notification.Verify(item => item.Create(It.IsAny<CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateComment_should_not_notify_when_mentions_list_is_empty()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7, userName: "Alice"));

            await service.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest
            {
                Body = "Olá",
                MentionedUserIds = new List<long>()
            });

            notification.Verify(item => item.Create(It.IsAny<CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateComment_should_not_notify_when_all_mentions_are_invalid_or_self()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7, userName: "Alice"));

            await service.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest
            {
                Body = "Olá",
                MentionedUserIds = new List<long> { 7, 0, -1 }
            });

            notification.Verify(item => item.Create(It.IsAny<CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateComment_should_truncate_long_body_in_notification_excerpt()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7, userName: "Alice"));

            string longBody = new('a', 200);

            await service.CreateComment(opportunity.Id, new CreateOpportunityCommentRequest
            {
                Body = longBody,
                MentionedUserIds = new List<long> { 10 }
            });

            notification.Verify(item => item.Create(It.Is<CreateNotificationRequest>(req =>
                req.Message.Contains("...") && req.Message.Length <= 200), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateComment_should_throw_when_comment_not_found()
        {
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7));
            Func<Task> act = () => service.UpdateComment(99, new UpdateOpportunityCommentRequest { Body = "x" });
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task DeleteComment_should_throw_when_comment_not_found()
        {
            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7));
            Func<Task> act = () => service.DeleteComment(99);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }
    }
}
