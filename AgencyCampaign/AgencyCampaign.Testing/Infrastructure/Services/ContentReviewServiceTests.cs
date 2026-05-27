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
    public sealed class ContentReviewServiceTests
    {
        private TestDbContext db = null!;
        private Mock<IContentFileStorage> fileStorage = null!;
        private ContentReviewService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            fileStorage = new Mock<IContentFileStorage>();
            service = new ContentReviewService(db, fileStorage.Object);
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

        private static AddContentVersionRequest BuildVersionRequest(string? note = null)
        {
            return new AddContentVersionRequest(
                new List<ContentAssetInput>
                {
                    new(ContentAssetType.ExternalUrl, "https://example.com/img.jpg", "img.jpg", "image/jpeg")
                },
                note
            );
        }

        [Test]
        public async Task AddVersion_first_round_should_create_version_with_round1_and_pending_internal_review()
        {
            long deliverableId = await SeedDeliverableAsync();

            ContentReviewModel result = await service.AddVersion(deliverableId, ReviewParticipant.Agency, "Maria", BuildVersionRequest("primeira versao"), CancellationToken.None);

            result.Versions.Should().HaveCount(1);
            result.Versions[0].RoundNumber.Should().Be(1);
            result.Versions[0].Status.Should().Be(ContentVersionStatus.PendingInternalReview);
            result.Versions[0].SubmittedByRole.Should().Be(ReviewParticipant.Agency);
            result.Versions[0].SubmittedByName.Should().Be("Maria");
        }

        [Test]
        public async Task AddVersion_after_request_changes_should_increment_to_round2()
        {
            long deliverableId = await SeedDeliverableAsync();

            ContentReviewModel v1 = await service.AddVersion(deliverableId, ReviewParticipant.Agency, "Maria", BuildVersionRequest(), CancellationToken.None);
            long versionId = v1.Versions[0].Id;

            await service.RequestChanges(versionId, "Revisora", "Falta legenda", CancellationToken.None);

            ContentReviewModel v2 = await service.AddVersion(deliverableId, ReviewParticipant.Agency, "Maria", BuildVersionRequest("ajustada"), CancellationToken.None);

            v2.Versions.Should().HaveCount(2);
            v2.Versions[1].RoundNumber.Should().Be(2);
            v2.Versions[1].Status.Should().Be(ContentVersionStatus.PendingInternalReview);
        }

        [Test]
        public async Task AddVersion_while_current_version_is_open_should_throw()
        {
            long deliverableId = await SeedDeliverableAsync();

            await service.AddVersion(deliverableId, ReviewParticipant.Agency, "Maria", BuildVersionRequest(), CancellationToken.None);

            Func<Task> act = () => service.AddVersion(deliverableId, ReviewParticipant.Agency, "Maria", BuildVersionRequest(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("contentReview.version.alreadyOpen");
        }
    }
}
