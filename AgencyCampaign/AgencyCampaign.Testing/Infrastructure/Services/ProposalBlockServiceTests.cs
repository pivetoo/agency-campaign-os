using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProposalBlockServiceTests
    {
        private TestDbContext db = null!;
        private ProposalBlockService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new ProposalBlockService(db, CurrentUserMock.Create(), LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_persist()
        {
            ProposalBlockModel result = await service.Create(new CreateProposalBlockRequest
            {
                Name = "Termos",
                Body = "Cláusulas...",
                Category = "Cláusulas"
            });

            result.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateProposalBlockRequest request = new() { Id = 5, Name = "x", Body = "x", Category = "x" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_throw_when_not_found()
        {
            UpdateProposalBlockRequest request = new() { Id = 99, Name = "x", Body = "x", Category = "x" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Delete_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Delete(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetAll_should_filter_by_category_and_inactive()
        {
            db.Add(new ProposalBlock("Termo A", "body", "termos", null, null));
            db.Add(new ProposalBlock("Termo B", "body", "termos", null, null));
            db.Add(new ProposalBlock("Cobertura", "body", "escopo", null, null));
            ProposalBlock inactive = new("Inactive", "body", "termos", null, null);
            inactive.Update("Inactive", "body", "termos", false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            (await service.GetAll(category: "termos", includeInactive: false)).Should().HaveCount(2);
            (await service.GetAll(category: "termos", includeInactive: true)).Should().HaveCount(3);
            (await service.GetAll(category: null, includeInactive: false)).Should().HaveCount(3);
        }
    }
}
