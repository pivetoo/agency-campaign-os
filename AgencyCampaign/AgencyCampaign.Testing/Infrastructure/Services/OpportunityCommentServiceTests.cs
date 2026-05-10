using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class OpportunityCommentServiceTests
    {
        private TestDbContext db = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private OpportunityCommentService BuildService(ICurrentUser currentUser)
        {
            return new OpportunityCommentService(db, currentUser, LocalizerMock.Create<AgencyCampaignResource>());
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
        public async Task DeleteComment_should_remove_when_user_is_author()
        {
            Opportunity opportunity = await SeedOpportunityAsync();
            OpportunityComment comment = new(opportunity.Id, "body", authorUserId: 7, authorName: "Alice");
            db.Add(comment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            OpportunityCommentService service = BuildService(CurrentUserMock.Create(userId: 7));
            await service.DeleteComment(comment.Id);

            (await db.Set<OpportunityComment>().CountAsync()).Should().Be(0);
        }

        [Test]
        public async Task GetByOpportunityId_should_throw_when_opportunity_not_found()
        {
            OpportunityCommentService service = BuildService(CurrentUserMock.Create());
            Func<Task> act = () => service.GetByOpportunityId(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
