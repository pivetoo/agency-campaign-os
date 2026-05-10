using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class BrandServiceTests
    {
        private TestDbContext db = null!;
        private BrandService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new BrandService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task CreateBrand_should_persist_with_normalized_fields()
        {
            CreateBrandRequest request = new() { Name = "  Acme  ", ContactEmail = "x@y" };

            Brand brand = await service.CreateBrand(request);

            brand.Name.Should().Be("Acme");
            (await db.Set<Brand>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task UpdateBrand_should_throw_when_id_mismatch()
        {
            UpdateBrandRequest request = new() { Id = 5, Name = "x" };

            Func<Task> act = () => service.UpdateBrand(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateBrand_should_throw_when_brand_not_found()
        {
            UpdateBrandRequest request = new() { Id = 1, Name = "x" };

            Func<Task> act = () => service.UpdateBrand(1, request);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdateBrand_should_replace_state()
        {
            Brand brand = new("Old");
            db.Add(brand);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateBrandRequest request = new()
            {
                Id = brand.Id,
                Name = "New",
                TradeName = "TT",
                IsActive = false
            };

            Brand result = await service.UpdateBrand(brand.Id, request);

            result.Name.Should().Be("New");
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task GetBrands_should_order_active_first_then_id_descending()
        {
            db.Add(new Brand("A").WithId(1));
            Brand inactive = new Brand("Inactive").WithId(2);
            inactive.Update("Inactive", null, null, null, null, null, false);
            db.Add(inactive);
            db.Add(new Brand("B").WithId(3));
            await db.SaveChangesAsync();

            PagedResult<Brand> result = await service.GetBrands(new PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Select(item => item.Name).Should().Equal("B", "A", "Inactive");
        }

        [Test]
        public async Task SetBrandLogo_should_persist_logo_url()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Brand result = await service.SetBrandLogo(brand.Id, "https://logo");

            result.LogoUrl.Should().Be("https://logo");
        }

        [Test]
        public async Task SetBrandLogo_should_throw_when_brand_not_found()
        {
            Func<Task> act = () => service.SetBrandLogo(99, "https://logo");
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task RemoveBrandLogo_should_clear_logo_url()
        {
            Brand brand = new("Acme");
            brand.SetLogo("https://logo");
            db.Add(brand);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Brand result = await service.RemoveBrandLogo(brand.Id);

            result.LogoUrl.Should().BeNull();
        }
    }
}
