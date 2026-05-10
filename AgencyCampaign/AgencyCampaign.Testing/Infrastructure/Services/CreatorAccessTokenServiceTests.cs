using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorAccessTokens;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CreatorAccessTokenServiceTests
    {
        private TestDbContext db = null!;
        private CreatorAccessTokenService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CreatorAccessTokenService(db, LocalizerMock.Create<AgencyCampaignResource>(), CurrentUserMock.Create());
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
        }

        [Test]
        public async Task Issue_should_throw_when_creator_does_not_exist()
        {
            IssueCreatorAccessTokenRequest request = new() { CreatorId = 99 };

            Func<Task> act = () => service.Issue(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Issue_should_persist_token_with_current_user_metadata()
        {
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();

            IssueCreatorAccessTokenRequest request = new() { CreatorId = creator.Id, Note = "p1" };

            CreatorAccessToken token = await service.Issue(request);

            token.CreatorId.Should().Be(creator.Id);
            token.Note.Should().Be("p1");
            token.Token.Should().NotBeNullOrWhiteSpace();
            token.CreatedByUserId.Should().Be(1);
            token.CreatedByUserName.Should().Be("Tester");

            (await db.Set<CreatorAccessToken>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task GetByCreator_should_filter_by_creator_id_and_order_descending()
        {
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();

            db.Add(new CreatorAccessToken(creator.Id, "tok-a").WithId(1));
            db.Add(new CreatorAccessToken(creator.Id, "tok-b").WithId(2));
            db.Add(new CreatorAccessToken(99, "outro").WithId(3));
            await db.SaveChangesAsync();

            List<CreatorAccessToken> tokens = await service.GetByCreator(creator.Id);

            tokens.Select(item => item.Token).Should().Equal("tok-b", "tok-a");
        }

        [Test]
        public async Task ValidateToken_should_return_null_for_blank()
        {
            (await service.ValidateToken(" ")).Should().BeNull();
            (await service.ValidateToken("")).Should().BeNull();
        }

        [Test]
        public async Task ValidateToken_should_return_null_when_token_not_found()
        {
            (await service.ValidateToken("missing")).Should().BeNull();
        }

        [Test]
        public async Task ValidateToken_should_return_null_for_expired_token()
        {
            CreatorAccessToken token = new(1, "abc", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
            db.Add(token);
            await db.SaveChangesAsync();

            (await service.ValidateToken("abc")).Should().BeNull();
        }

        [Test]
        public async Task ValidateToken_should_increment_usage_for_valid_token()
        {
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();

            CreatorAccessToken token = new(creator.Id, "abc", expiresAt: DateTimeOffset.UtcNow.AddDays(1));
            db.Add(token);
            await db.SaveChangesAsync();

            CreatorAccessToken? returned = await service.ValidateToken("abc");

            returned.Should().NotBeNull();
            returned!.UsageCount.Should().Be(1);
            returned.LastUsedAt.Should().NotBeNull();

            db.ChangeTracker.Clear();
            CreatorAccessToken persisted = await db.Set<CreatorAccessToken>().SingleAsync();
            persisted.UsageCount.Should().Be(1);
        }

        [Test]
        public async Task Revoke_should_return_false_when_not_found()
        {
            (await service.Revoke(99)).Should().BeFalse();
        }

        [Test]
        public async Task Revoke_should_mark_token_as_revoked()
        {
            CreatorAccessToken token = new(1, "abc");
            db.Add(token);
            await db.SaveChangesAsync();

            bool result = await service.Revoke(token.Id);

            result.Should().BeTrue();
            db.ChangeTracker.Clear();
            CreatorAccessToken persisted = await db.Set<CreatorAccessToken>().SingleAsync();
            persisted.RevokedAt.Should().NotBeNull();
        }
    }
}
