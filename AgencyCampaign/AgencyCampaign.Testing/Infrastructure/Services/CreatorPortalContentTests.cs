using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Moq;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CreatorPortalContentTests
    {
        private TestDbContext db = null!;
        private ContentReviewService contentReview = null!;
        private Mock<ICreatorAccessTokenService> accessTokenMock = null!;
        private CreatorPortalService portalService = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            contentReview = new ContentReviewService(db);
            accessTokenMock = new Mock<ICreatorAccessTokenService>();
            portalService = new CreatorPortalService(db, accessTokenMock.Object, contentReview);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<(long creatorId, long deliverableId)> SeedAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "Campanha Teste", 0m, DateTimeOffset.UtcNow).WithId(10));
            Creator creator = new Creator("Criador Teste", "criador_stage");
            creator.WithId(1);
            db.Add(creator);
            db.Add(new CampaignCreator(10, 1, 1, 100m, 10m).WithId(20));
            db.Add(new Platform("IG").WithId(1));
            db.Add(new DeliverableKind("Story").WithId(1));
            CampaignDeliverable deliverable = new(10, 20, "Entrega Teste", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            deliverable.WithId(50);
            db.Add(deliverable);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return (1L, 50L);
        }

        private static AddContentVersionRequest BuildVersionRequest()
        {
            return new AddContentVersionRequest(
                new List<ContentAssetInput>
                {
                    new(ContentAssetType.ExternalUrl, "https://example.com/img.jpg", "img.jpg", "image/jpeg")
                },
                null
            );
        }

        [Test]
        public async Task GetDeliverableReview_returns_only_shared_comments()
        {
            (long creatorId, long deliverableId) = await SeedAsync();

            await contentReview.AddComment(deliverableId, ReviewParticipant.Agency, "Agencia", new AddReviewCommentRequest(null, "Comentario interno", ReviewCommentVisibility.Internal), CancellationToken.None);
            await contentReview.AddComment(deliverableId, ReviewParticipant.Agency, "Agencia", new AddReviewCommentRequest(null, "Comentario compartilhado", ReviewCommentVisibility.Shared), CancellationToken.None);
            db.ChangeTracker.Clear();

            ContentReviewModel result = await portalService.GetDeliverableReview(creatorId, deliverableId, CancellationToken.None);

            result.Comments.Should().HaveCount(1);
            result.Comments[0].Body.Should().Be("Comentario compartilhado");
            result.Comments[0].Visibility.Should().Be(ReviewCommentVisibility.Shared);
        }

        [Test]
        public async Task SubmitContentVersion_creates_version_with_creator_role_and_pending_internal_review()
        {
            (long creatorId, long deliverableId) = await SeedAsync();

            ContentReviewModel result = await portalService.SubmitContentVersion(creatorId, deliverableId, BuildVersionRequest(), CancellationToken.None);

            result.Versions.Should().HaveCount(1);
            result.Versions[0].SubmittedByRole.Should().Be(ReviewParticipant.Creator);
            result.Versions[0].Status.Should().Be(ContentVersionStatus.PendingInternalReview);
            result.Versions[0].RoundNumber.Should().Be(1);
            result.Versions[0].SubmittedByName.Should().Be("criador_stage");
        }

        [Test]
        public async Task GetDeliverableReview_throws_when_deliverable_does_not_belong_to_creator()
        {
            await SeedAsync();
            long wrongCreatorId = 999L;

            Func<Task> act = () => portalService.GetDeliverableReview(wrongCreatorId, 50L, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("record.notFound");
        }
    }
}
