using AgencyCampaign.Application.Models.Creators;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainEntities = AgencyCampaign.Domain.Entities;
using CampaignCreatorStatus = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;

namespace AgencyCampaign.IntegrationTests.Services
{
    // Cobertura de integracao do ICreatorService inteiro contra Postgres real: CRUD + busca, fotos,
    // e os agregados que cruzam o grafo de campanha (GetSummary / GetCampaignsByCreator) semeados via
    // DbContext real em ordem de FK (identidade atribuida pelo banco, sem WithId).
    [TestFixture]
    public sealed class CreatorServiceIntegrationTests : IntegrationTestBase
    {
        private static async Task<long> SeedCreatorAsync(IServiceProvider serviceProvider, string name, string? stageName = null, bool isActive = true)
        {
            ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();
            Creator created = await service.CreateCreator(new CreateCreatorRequest { Name = name, StageName = stageName });

            if (!isActive)
            {
                await service.UpdateCreator(created.Id, new UpdateCreatorRequest { Id = created.Id, Name = name, StageName = stageName, IsActive = false });
            }

            return created.Id;
        }

        [Test]
        public async Task CreateCreator_persists_normalized_with_tax_regime()
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
                Creator? fetched = await serviceProvider.GetRequiredService<ICreatorService>().GetCreatorById(id);

                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("Foo");
                fetched.TaxRegime.Should().Be(TaxRegime.SimplesNacional);
            });
        }

        [Test]
        public async Task GetCreatorById_returns_null_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Creator? fetched = await serviceProvider.GetRequiredService<ICreatorService>().GetCreatorById(99999);
                fetched.Should().BeNull();
            });
        }

        [Test]
        public async Task UpdateCreator_persists_changes()
        {
            long id = 0;
            await InScopeAsync(async serviceProvider => id = await SeedCreatorAsync(serviceProvider, "Alice"));

            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                Creator updated = await service.UpdateCreator(id, new UpdateCreatorRequest { Id = id, Name = "Updated", Email = "new@x.test" });

                updated.Name.Should().Be("Updated");
                updated.Email.Should().Be("new@x.test");
            });

            await InScopeAsync(async serviceProvider =>
            {
                Creator? fetched = await serviceProvider.GetRequiredService<ICreatorService>().GetCreatorById(id);
                fetched!.Email.Should().Be("new@x.test");
            });
        }

        [Test]
        public async Task UpdateCreator_throws_when_id_mismatch()
        {
            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                Func<Task> act = () => service.UpdateCreator(99, new UpdateCreatorRequest { Id = 5, Name = "x" });

                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task UpdateCreator_throws_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                Func<Task> act = () => service.UpdateCreator(4242, new UpdateCreatorRequest { Id = 4242, Name = "x" });

                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task GetCreators_filters_inactive_by_default()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedCreatorAsync(serviceProvider, "Active One");
                await SeedCreatorAsync(serviceProvider, "Inactive One", isActive: false);
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                PagedResult<Creator> result = await service.GetCreators(new PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: false);

                result.Items.Should().ContainSingle(item => item.Name == "Active One");
            });
        }

        [Test]
        public async Task GetCreators_searches_by_name_or_stage_name()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedCreatorAsync(serviceProvider, "Joana Silva", stageName: "JoSilva");
                await SeedCreatorAsync(serviceProvider, "Outro Nome", stageName: "Outro");
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

                PagedResult<Creator> byName = await service.GetCreators(new PagedRequest { Page = 1, PageSize = 10 }, "joana", includeInactive: true);
                PagedResult<Creator> byStage = await service.GetCreators(new PagedRequest { Page = 1, PageSize = 10 }, "josilva", includeInactive: true);

                byName.Items.Should().ContainSingle(item => item.StageName == "JoSilva");
                byStage.Items.Should().ContainSingle(item => item.StageName == "JoSilva");
            });
        }

        [Test]
        public async Task SetCreatorPhoto_persists_photo_url()
        {
            long id = 0;
            await InScopeAsync(async serviceProvider => id = await SeedCreatorAsync(serviceProvider, "Photo Creator"));

            await InScopeAsync(async serviceProvider =>
            {
                Creator result = await serviceProvider.GetRequiredService<ICreatorService>().SetCreatorPhoto(id, "/uploads/photo.jpg");
                result.PhotoUrl.Should().Be("/uploads/photo.jpg");
            });

            await InScopeAsync(async serviceProvider =>
            {
                Creator? fetched = await serviceProvider.GetRequiredService<ICreatorService>().GetCreatorById(id);
                fetched!.PhotoUrl.Should().Be("/uploads/photo.jpg");
            });
        }

        [Test]
        public async Task SetCreatorPhoto_throws_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Func<Task> act = () => serviceProvider.GetRequiredService<ICreatorService>().SetCreatorPhoto(99999, "/p.jpg");
                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task RemoveCreatorPhoto_clears_photo_url()
        {
            long id = 0;
            await InScopeAsync(async serviceProvider =>
            {
                id = await SeedCreatorAsync(serviceProvider, "Clear Photo");
                await serviceProvider.GetRequiredService<ICreatorService>().SetCreatorPhoto(id, "/p.jpg");
            });

            await InScopeAsync(async serviceProvider =>
            {
                Creator result = await serviceProvider.GetRequiredService<ICreatorService>().RemoveCreatorPhoto(id);
                result.PhotoUrl.Should().BeNull();
            });
        }

        [Test]
        public async Task GetSummary_returns_null_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                CreatorSummaryModel? summary = await serviceProvider.GetRequiredService<ICreatorService>().GetSummary(99999);
                summary.Should().BeNull();
            });
        }

        [Test]
        public async Task GetSummary_returns_zero_metrics_for_creator_without_campaigns()
        {
            long id = 0;
            await InScopeAsync(async serviceProvider => id = await SeedCreatorAsync(serviceProvider, "Solo"));

            await InScopeAsync(async serviceProvider =>
            {
                CreatorSummaryModel? summary = await serviceProvider.GetRequiredService<ICreatorService>().GetSummary(id);

                summary.Should().NotBeNull();
                summary!.CreatorId.Should().Be(id);
                summary.TotalCampaigns.Should().Be(0);
                summary.TotalDeliverables.Should().Be(0);
                summary.OnTimeDeliveryRate.Should().Be(0);
                summary.PerformanceByPlatform.Should().BeEmpty();
            });
        }

        [Test]
        public async Task GetSummary_computes_aggregated_metrics_over_campaign_graph()
        {
            long creatorId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                DbContext db = serviceProvider.GetRequiredService<DbContext>();
                DateTimeOffset now = DateTimeOffset.UtcNow;

                Brand brand = new("Acme");
                db.Add(brand);
                await db.SaveChangesAsync();

                Campaign campaignOne = new(brand.Id, "C1", 0m, now);
                Campaign campaignTwo = new(brand.Id, "C2", 0m, now);
                Creator creator = new("Foo");
                Platform platform = new("Instagram");
                DeliverableKind kind = new("Story");
                CampaignCreatorStatus confirmedStatus = new("Confirmado", 1, "#fff", category: CampaignCreatorStatusCategory.Success, marksAsConfirmed: true);
                db.Add(campaignOne);
                db.Add(campaignTwo);
                db.Add(creator);
                db.Add(platform);
                db.Add(kind);
                db.Add(confirmedStatus);
                await db.SaveChangesAsync();

                DomainEntities.CampaignCreator confirmed = new(campaignOne.Id, creator.Id, confirmedStatus.Id, 100m, 10m);
                confirmed.ChangeStatus(confirmedStatus);
                DomainEntities.CampaignCreator open = new(campaignTwo.Id, creator.Id, confirmedStatus.Id, 100m, 10m);
                db.Add(confirmed);
                db.Add(open);
                await db.SaveChangesAsync();

                CampaignDeliverable onTime = new(campaignOne.Id, confirmed.Id, "Deliverable 1", kind.Id, platform.Id, now.AddDays(1), 100m, 80m, 10m);
                onTime.Publish("https://post.test/1", null, now.AddDays(-1));
                CampaignDeliverable overdue = new(campaignTwo.Id, open.Id, "Deliverable 2", kind.Id, platform.Id, now.AddDays(-2), 100m, 80m, 10m);
                db.Add(onTime);
                db.Add(overdue);
                await db.SaveChangesAsync();

                creatorId = creator.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                CreatorSummaryModel? summary = await serviceProvider.GetRequiredService<ICreatorService>().GetSummary(creatorId);

                summary.Should().NotBeNull();
                summary!.TotalCampaigns.Should().Be(2);
                summary.ConfirmedCampaigns.Should().Be(1);
                summary.TotalDeliverables.Should().Be(2);
                summary.PublishedDeliverables.Should().Be(1);
                summary.OverdueDeliverables.Should().Be(1);
                summary.OnTimeDeliveryRate.Should().Be(100m);
                summary.PerformanceByPlatform.Should().ContainSingle();
            });
        }

        [Test]
        public async Task GetCampaignsByCreator_returns_empty_when_none()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IReadOnlyCollection<DomainEntities.CampaignCreator> result = await serviceProvider.GetRequiredService<ICreatorService>().GetCampaignsByCreator(99999);
                result.Should().BeEmpty();
            });
        }

        [Test]
        public async Task GetCampaignsByCreator_returns_associations_with_campaign_and_brand()
        {
            long creatorId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                DbContext db = serviceProvider.GetRequiredService<DbContext>();

                Brand brand = new("Acme");
                db.Add(brand);
                await db.SaveChangesAsync();

                Campaign campaign = new(brand.Id, "Campaign X", 0m, DateTimeOffset.UtcNow);
                Creator creator = new("Foo");
                CampaignCreatorStatus status = new("Aberto", 1, "#fff");
                db.Add(campaign);
                db.Add(creator);
                db.Add(status);
                await db.SaveChangesAsync();

                DomainEntities.CampaignCreator association = new(campaign.Id, creator.Id, status.Id, 100m, 10m);
                db.Add(association);
                await db.SaveChangesAsync();

                creatorId = creator.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                IReadOnlyCollection<DomainEntities.CampaignCreator> result = await serviceProvider.GetRequiredService<ICreatorService>().GetCampaignsByCreator(creatorId);

                result.Should().ContainSingle();
                DomainEntities.CampaignCreator association = result.First();
                association.Campaign.Should().NotBeNull();
                association.Campaign!.Brand.Should().NotBeNull();
                association.Campaign.Brand!.Name.Should().Be("Acme");
            });
        }

        [Test]
        public async Task ExportAsync_yields_one_line_per_creator()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedCreatorAsync(serviceProvider, "Export A");
                await SeedCreatorAsync(serviceProvider, "Export B");
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICreatorService service = serviceProvider.GetRequiredService<ICreatorService>();

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
