using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ContentReviewBrandTests
    {
        private TestDbContext db = null!;
        private ContentReviewService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new ContentReviewService(db, MediaTokenTestFactory.Create());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<long> SeedDeliverableAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "Campanha Teste", 0m, DateTimeOffset.UtcNow).WithId(10));
            db.Add(new Creator("Criador Teste").WithId(1));
            db.Add(new CampaignCreator(10, 1, 1, 100m, 10m).WithId(20));
            db.Add(new Platform("IG").WithId(1));
            db.Add(new DeliverableKind("Story").WithId(1));
            CampaignDeliverable deliverable = new(10, 20, "Entrega Teste", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            deliverable.WithId(50);
            db.Add(deliverable);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return 50;
        }

        private async Task<long> SeedVersionInBrandReviewAsync(long deliverableId)
        {
            ContentReviewModel model = await service.AddVersion(deliverableId, ReviewParticipant.Agency, "Maria",
                new AddContentVersionRequest(
                    new List<ContentAssetInput> { new(ContentAssetType.ExternalUrl, "https://example.com/v.jpg", "v.jpg", "image/jpeg") },
                    null),
                CancellationToken.None);

            long versionId = model.Versions[0].Id;
            await service.SendToBrand(versionId, CancellationToken.None);
            return versionId;
        }

        [Test]
        public async Task BrandApprove_when_version_is_pending_brand_review_should_set_approved()
        {
            long deliverableId = await SeedDeliverableAsync();
            await SeedVersionInBrandReviewAsync(deliverableId);

            ContentReviewModel result = await service.BrandApprove(deliverableId, CancellationToken.None);

            result.Versions.Should().HaveCount(1);
            result.Versions[0].Status.Should().Be(ContentVersionStatus.Approved);
        }

        [Test]
        public async Task BrandApprove_when_no_version_in_brand_review_should_return_model_unchanged()
        {
            long deliverableId = await SeedDeliverableAsync();

            ContentReviewModel result = await service.BrandApprove(deliverableId, CancellationToken.None);

            result.Versions.Should().BeEmpty();
        }

        [Test]
        public async Task BrandApprove_returns_shared_comments_only()
        {
            long deliverableId = await SeedDeliverableAsync();
            await SeedVersionInBrandReviewAsync(deliverableId);

            db.Add(new DeliverableReviewComment(deliverableId, null, ReviewParticipant.Agency, "Maria", "Comentario interno", ReviewCommentVisibility.Internal));
            db.Add(new DeliverableReviewComment(deliverableId, null, ReviewParticipant.Agency, "Maria", "Comentario compartilhado", ReviewCommentVisibility.Shared));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            ContentReviewModel result = await service.BrandApprove(deliverableId, CancellationToken.None);

            result.Comments.Should().HaveCount(1);
            result.Comments[0].Body.Should().Be("Comentario compartilhado");
        }

        [Test]
        public async Task BrandRequestChanges_when_version_is_pending_brand_review_should_set_changes_requested_and_add_brand_shared_comment()
        {
            long deliverableId = await SeedDeliverableAsync();
            await SeedVersionInBrandReviewAsync(deliverableId);

            ContentReviewModel result = await service.BrandRequestChanges(deliverableId, "Acme Brand", "Precisa ajustar legenda", CancellationToken.None);

            result.Versions.Should().HaveCount(1);
            result.Versions[0].Status.Should().Be(ContentVersionStatus.ChangesRequested);
            result.Comments.Should().HaveCount(1);
            result.Comments[0].Body.Should().Be("Precisa ajustar legenda");
            result.Comments[0].AuthorRole.Should().Be(ReviewParticipant.Brand);
            result.Comments[0].Visibility.Should().Be(ReviewCommentVisibility.Shared);
        }

        [Test]
        public async Task BrandRequestChanges_when_no_version_in_brand_review_should_throw()
        {
            long deliverableId = await SeedDeliverableAsync();

            Func<Task> act = () => service.BrandRequestChanges(deliverableId, "Acme Brand", "Comentario", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("contentReview.version.invalidTransition");
        }

        [Test]
        public async Task BrandRequestChanges_returns_shared_comments_only()
        {
            long deliverableId = await SeedDeliverableAsync();
            await SeedVersionInBrandReviewAsync(deliverableId);

            db.Add(new DeliverableReviewComment(deliverableId, null, ReviewParticipant.Agency, "Maria", "Comentario interno", ReviewCommentVisibility.Internal));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            ContentReviewModel result = await service.BrandRequestChanges(deliverableId, "Acme Brand", "Solicito ajuste", CancellationToken.None);

            result.Comments.All(c => c.Visibility == ReviewCommentVisibility.Shared).Should().BeTrue();
        }

        [Test]
        public async Task BrandAddComment_should_add_brand_shared_comment_and_return_shared_only()
        {
            long deliverableId = await SeedDeliverableAsync();

            db.Add(new DeliverableReviewComment(deliverableId, null, ReviewParticipant.Agency, "Maria", "Interno", ReviewCommentVisibility.Internal));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            ContentReviewModel result = await service.BrandAddComment(deliverableId, "Acme Brand", "Meu comentario publico", CancellationToken.None);

            result.Comments.Should().HaveCount(1);
            result.Comments[0].Body.Should().Be("Meu comentario publico");
            result.Comments[0].AuthorRole.Should().Be(ReviewParticipant.Brand);
            result.Comments[0].Visibility.Should().Be(ReviewCommentVisibility.Shared);
        }
    }
}
