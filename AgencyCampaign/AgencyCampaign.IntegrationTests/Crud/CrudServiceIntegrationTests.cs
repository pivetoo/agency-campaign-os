using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace AgencyCampaign.IntegrationTests.Crud
{
    // Servicos CRUD que dependem so do DbContext (sem ICurrentUser): exercita o caminho real
    // servico -> EF -> Postgres -> leitura de volta. Cada create/read roda num scope DI separado
    // para provar a persistencia atravessando contextos (igual ao app real).
    [TestFixture]
    public sealed class CrudServiceIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task BrandService_creates_and_reads_back_with_normalized_name()
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
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                Brand? fetched = await service.GetBrandById(id);

                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("Acme");
                fetched.ContactEmail.Should().Be("contact@acme.test");
            });
        }

        [Test]
        public async Task BrandService_lists_created_brand()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();
                await service.CreateBrand(new CreateBrandRequest { Name = "Listed Brand" });
            });

            await InScopeAsync(async serviceProvider =>
            {
                IBrandService service = serviceProvider.GetRequiredService<IBrandService>();

                PagedResult<Brand> result = await service.GetBrands(new PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: true);

                result.Items.Should().ContainSingle(item => item.Name == "Listed Brand");
            });
        }

        [Test]
        public async Task CreatorService_creates_and_reads_back_with_normalized_data()
        {
            long id = 0;

            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                Creator created = await service.CreateCreator(new CreateCreatorRequest { Name = " Foo ", StageName = " Bar ", TaxRegime = TaxRegime.SimplesNacional });

                created.Id.Should().BeGreaterThan(0);
                created.Name.Should().Be("Foo");
                created.StageName.Should().Be("Bar");
                id = created.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                Creator? fetched = await service.GetCreatorById(id);

                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("Foo");
                fetched.TaxRegime.Should().Be(TaxRegime.SimplesNacional);
            });
        }

        [Test]
        public async Task CreatorService_searches_by_name_or_stage_name()
        {
            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();
                await service.CreateCreator(new CreateCreatorRequest { Name = "Joana Silva", StageName = "JoSilva" });
                await service.CreateCreator(new CreateCreatorRequest { Name = "Outro Nome", StageName = "Outro" });
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                PagedResult<Creator> byName = await service.GetCreators(new PagedRequest { Page = 1, PageSize = 10 }, "joana", includeInactive: true);

                byName.Items.Should().ContainSingle(item => item.StageName == "JoSilva");
            });
        }
    }
}
