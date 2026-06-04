using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace AgencyCampaign.IntegrationTests.Services
{
    // Cobertura de integracao do IBrandService inteiro contra Postgres real: create/update/get/list
    // (ordenacao, filtro e busca avaliados como SQL de verdade), logo e export. Cada cenario que prova
    // persistencia cria num scope e le de volta noutro, como o app real.
    [TestFixture]
    public sealed class BrandServiceIntegrationTests : IntegrationTestBase
    {
        private static async Task<long> SeedBrandAsync(IServiceProvider serviceProvider, string name, bool isActive = true)
        {
            IBrandService service = serviceProvider.GetRequiredService<IBrandService>();
            Brand created = await service.CreateBrand(new CreateBrandRequest { Name = name });

            if (!isActive)
            {
                await service.UpdateBrand(created.Id, new UpdateBrandRequest { Id = created.Id, Name = name, IsActive = false });
            }

            return created.Id;
        }

        [Test]
        public async Task CreateBrand_persists_normalized_and_assigns_identity()
        {
            long id = 0;

            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                Brand created = await service.CreateBrand(new CreateBrandRequest { Name = "  Acme  ", ContactEmail = "contact@acme.test" });

                created.Id.Should().BeGreaterThan(0);
                created.Name.Should().Be("Acme");
                id = created.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                Brand? fetched = await serviceProvider.GetRequiredService<IBrandService>().GetBrandById(id);

                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("Acme");
                fetched.ContactEmail.Should().Be("contact@acme.test");
                fetched.IsActive.Should().BeTrue();
            });
        }

        [Test]
        public async Task GetBrandById_returns_null_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Brand? fetched = await serviceProvider.GetRequiredService<IBrandService>().GetBrandById(99999);
                fetched.Should().BeNull();
            });
        }

        [Test]
        public async Task UpdateBrand_replaces_state()
        {
            long id = 0;
            await InScopeAsync(async serviceProvider => id = await SeedBrandAsync(serviceProvider, "Old Name"));

            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                Brand updated = await service.UpdateBrand(id, new UpdateBrandRequest
                {
                    Id = id,
                    Name = "New Name",
                    TradeName = "NN",
                    IsActive = false
                });

                updated.Name.Should().Be("New Name");
                updated.TradeName.Should().Be("NN");
                updated.IsActive.Should().BeFalse();
            });

            await InScopeAsync(async serviceProvider =>
            {
                Brand? fetched = await serviceProvider.GetRequiredService<IBrandService>().GetBrandById(id);
                fetched!.Name.Should().Be("New Name");
                fetched.IsActive.Should().BeFalse();
            });
        }

        [Test]
        public async Task UpdateBrand_throws_when_id_mismatch()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                Func<Task> act = () => service.UpdateBrand(99, new UpdateBrandRequest { Id = 5, Name = "x" });

                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task UpdateBrand_throws_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                Func<Task> act = () => service.UpdateBrand(4242, new UpdateBrandRequest { Id = 4242, Name = "x" });

                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task GetBrands_orders_active_first_then_id_descending()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedBrandAsync(serviceProvider, "Alpha");
                await SeedBrandAsync(serviceProvider, "Beta", isActive: false);
                await SeedBrandAsync(serviceProvider, "Gamma");
            });

            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                PagedResult<Brand> result = await service.GetBrands(new PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: true);

                // Ativos primeiro por id desc (Gamma > Alpha), depois o inativo (Beta).
                result.Items.Select(item => item.Name).Should().Equal("Gamma", "Alpha", "Beta");
            });
        }

        [Test]
        public async Task GetBrands_filters_inactive_when_not_included()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedBrandAsync(serviceProvider, "VisibleActive");
                await SeedBrandAsync(serviceProvider, "HiddenInactive", isActive: false);
            });

            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                PagedResult<Brand> result = await service.GetBrands(new PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: false);

                result.Items.Should().ContainSingle(item => item.Name == "VisibleActive");
            });
        }

        [Test]
        public async Task GetBrands_applies_search_filter()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedBrandAsync(serviceProvider, "Acme Corporation");
                await SeedBrandAsync(serviceProvider, "Outra Empresa");
            });

            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                PagedResult<Brand> result = await service.GetBrands(new PagedRequest { Page = 1, PageSize = 10 }, "acme", includeInactive: true);

                result.Items.Should().ContainSingle();
                result.Items.First().Name.Should().Be("Acme Corporation");
            });
        }

        [Test]
        public async Task SetBrandLogo_persists_logo_url()
        {
            long id = 0;
            await InScopeAsync(async serviceProvider => id = await SeedBrandAsync(serviceProvider, "WithLogo"));

            await InScopeAsync(async serviceProvider =>
            {
                Brand result = await serviceProvider.GetRequiredService<IBrandService>().SetBrandLogo(id, "https://logo.test/a.png");
                result.LogoUrl.Should().Be("https://logo.test/a.png");
            });

            await InScopeAsync(async serviceProvider =>
            {
                Brand? fetched = await serviceProvider.GetRequiredService<IBrandService>().GetBrandById(id);
                fetched!.LogoUrl.Should().Be("https://logo.test/a.png");
            });
        }

        [Test]
        public async Task SetBrandLogo_throws_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Func<Task> act = () => serviceProvider.GetRequiredService<IBrandService>().SetBrandLogo(99999, "https://logo");
                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task RemoveBrandLogo_clears_logo_url()
        {
            long id = 0;
            await InScopeAsync(async serviceProvider =>
            {
                id = await SeedBrandAsync(serviceProvider, "ClearLogo");
                await serviceProvider.GetRequiredService<IBrandService>().SetBrandLogo(id, "https://logo.test/a.png");
            });

            await InScopeAsync(async serviceProvider =>
            {
                Brand result = await serviceProvider.GetRequiredService<IBrandService>().RemoveBrandLogo(id);
                result.LogoUrl.Should().BeNull();
            });
        }

        [Test]
        public async Task RemoveBrandLogo_throws_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Func<Task> act = () => serviceProvider.GetRequiredService<IBrandService>().RemoveBrandLogo(99999);
                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task ExportAsync_yields_one_line_per_brand()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedBrandAsync(serviceProvider, "Export A");
                await SeedBrandAsync(serviceProvider, "Export B");
            });

            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                List<string> lines = new();
                await foreach (string line in service.ExportAsync())
                {
                    lines.Add(line);
                }

                lines.Should().HaveCount(2);
            });
        }
    }
}
