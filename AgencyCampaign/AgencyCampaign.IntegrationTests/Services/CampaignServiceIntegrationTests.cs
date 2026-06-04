using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Application.Requests.Campaigns;
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
    // Cobertura de integracao do ICampaignService inteiro contra Postgres real: create (com validacao de
    // brand e historico de status), update (mismatch/ausente/mudanca de status), listagem/busca, summary
    // agregado e historico. Exercita ICurrentUser (historico) e IdentityUsersClient (inerte) via DI real.
    [TestFixture]
    public sealed class CampaignServiceIntegrationTests : IntegrationTestBase
    {
        private static async Task<long> SeedBrandAsync(IServiceProvider serviceProvider, string name = "Acme")
        {
            Brand brand = await serviceProvider.GetRequiredService<IBrandService>().CreateBrand(new CreateBrandRequest { Name = name });
            return brand.Id;
        }

        private static CreateCampaignRequest NewCampaignRequest(long brandId, string name = "Campaign X", CampaignStatus status = CampaignStatus.Draft, decimal budget = 1000m) =>
            new() { BrandId = brandId, Name = name, Budget = budget, StartsAt = DateTimeOffset.UtcNow, Status = status };

        [Test]
        public async Task CreateCampaign_persists_and_reads_back_with_brand()
        {
            long campaignId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                long brandId = await SeedBrandAsync(serviceProvider);
                Campaign created = await serviceProvider.GetRequiredService<ICampaignService>().CreateCampaign(NewCampaignRequest(brandId, "Launch"));

                created.Id.Should().BeGreaterThan(0);
                campaignId = created.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                Campaign? fetched = await serviceProvider.GetRequiredService<ICampaignService>().GetCampaignById(campaignId);

                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("Launch");
                fetched.Brand.Should().NotBeNull();
                fetched.Brand!.Name.Should().Be("Acme");
            });
        }

        [Test]
        public async Task CreateCampaign_throws_when_brand_not_found()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Func<Task> act = () => serviceProvider.GetRequiredService<ICampaignService>().CreateCampaign(NewCampaignRequest(99999));
                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task CreateCampaign_records_initial_status_history()
        {
            long campaignId = 0;
            await InScopeAsync(async serviceProvider =>
            {
                long brandId = await SeedBrandAsync(serviceProvider);
                Campaign created = await serviceProvider.GetRequiredService<ICampaignService>().CreateCampaign(NewCampaignRequest(brandId));
                campaignId = created.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                IReadOnlyCollection<CampaignStatusHistory> history = await serviceProvider.GetRequiredService<ICampaignService>().GetStatusHistory(campaignId);
                history.Should().ContainSingle();
            });
        }

        [Test]
        public async Task GetCampaignById_returns_null_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (await serviceProvider.GetRequiredService<ICampaignService>().GetCampaignById(99999)).Should().BeNull();
            });
        }

        [Test]
        public async Task UpdateCampaign_throws_when_id_mismatch()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Func<Task> act = () => serviceProvider.GetRequiredService<ICampaignService>().UpdateCampaign(1, new UpdateCampaignRequest { Id = 2, BrandId = 1, Name = "x", StartsAt = DateTimeOffset.UtcNow });
                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task UpdateCampaign_throws_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                Func<Task> act = () => serviceProvider.GetRequiredService<ICampaignService>().UpdateCampaign(4242, new UpdateCampaignRequest { Id = 4242, BrandId = 1, Name = "x", StartsAt = DateTimeOffset.UtcNow });
                await act.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Test]
        public async Task UpdateCampaign_persists_changes_and_records_status_change()
        {
            long brandId = 0;
            long campaignId = 0;
            await InScopeAsync(async serviceProvider =>
            {
                brandId = await SeedBrandAsync(serviceProvider);
                Campaign created = await serviceProvider.GetRequiredService<ICampaignService>().CreateCampaign(NewCampaignRequest(brandId, "Before", CampaignStatus.Draft));
                campaignId = created.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                Campaign updated = await serviceProvider.GetRequiredService<ICampaignService>().UpdateCampaign(campaignId, new UpdateCampaignRequest
                {
                    Id = campaignId,
                    BrandId = brandId,
                    Name = "After",
                    Budget = 500m,
                    StartsAt = DateTimeOffset.UtcNow,
                    Status = CampaignStatus.InProgress
                });

                updated.Name.Should().Be("After");
                updated.Status.Should().Be(CampaignStatus.InProgress);
            });

            await InScopeAsync(async serviceProvider =>
            {
                IReadOnlyCollection<CampaignStatusHistory> history = await serviceProvider.GetRequiredService<ICampaignService>().GetStatusHistory(campaignId);
                // 1 do create (null -> Draft) + 1 da mudanca (Draft -> InProgress).
                history.Should().HaveCount(2);
            });
        }

        [Test]
        public async Task GetCampaigns_orders_active_first_then_id_descending_and_searches()
        {
            await InScopeAsync(async serviceProvider =>
            {
                long brandId = await SeedBrandAsync(serviceProvider);
                ICampaignService service = serviceProvider.GetRequiredService<ICampaignService>();
                await service.CreateCampaign(NewCampaignRequest(brandId, "Alpha"));
                await service.CreateCampaign(NewCampaignRequest(brandId, "Beta"));
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICampaignService service = serviceProvider.GetRequiredService<ICampaignService>();

                PagedResult<Campaign> all = await service.GetCampaigns(new PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: true);
                all.Items.Select(item => item.Name).Should().Equal("Beta", "Alpha");

                PagedResult<Campaign> searched = await service.GetCampaigns(new PagedRequest { Page = 1, PageSize = 10 }, "alph", includeInactive: true);
                searched.Items.Should().ContainSingle(item => item.Name == "Alpha");
            });
        }

        [Test]
        public async Task GetSummary_returns_null_when_absent()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (await serviceProvider.GetRequiredService<ICampaignService>().GetSummary(99999)).Should().BeNull();
            });
        }

        [Test]
        public async Task GetSummary_aggregates_campaign_metrics_over_deliverables()
        {
            long campaignId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                long brandId = await SeedBrandAsync(serviceProvider);
                Campaign campaign = await serviceProvider.GetRequiredService<ICampaignService>().CreateCampaign(NewCampaignRequest(brandId, "Metrics", CampaignStatus.InProgress, budget: 1000m));
                campaignId = campaign.Id;

                DbContext db = serviceProvider.GetRequiredService<DbContext>();
                Creator creator = new("Foo");
                Platform platform = new("Instagram");
                DeliverableKind kind = new("Story");
                CampaignCreatorStatus status = new("Aberto", 1, "#fff");
                db.Add(creator);
                db.Add(platform);
                db.Add(kind);
                db.Add(status);
                await db.SaveChangesAsync();

                DomainEntities.CampaignCreator campaignCreator = new(campaignId, creator.Id, status.Id, 100m, 10m);
                db.Add(campaignCreator);
                await db.SaveChangesAsync();

                CampaignDeliverable published = new(campaignId, campaignCreator.Id, "D1", kind.Id, platform.Id, DateTimeOffset.UtcNow.AddDays(1), 200m, 160m, 20m);
                published.Publish("https://post.test/1", null, DateTimeOffset.UtcNow);
                CampaignDeliverable pending = new(campaignId, campaignCreator.Id, "D2", kind.Id, platform.Id, DateTimeOffset.UtcNow.AddDays(2), 100m, 80m, 10m);
                db.Add(published);
                db.Add(pending);
                await db.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                CampaignSummaryModel? summary = await serviceProvider.GetRequiredService<ICampaignService>().GetSummary(campaignId);

                summary.Should().NotBeNull();
                summary!.CampaignCreatorsCount.Should().Be(1);
                summary.DeliverablesCount.Should().Be(2);
                summary.PublishedDeliverablesCount.Should().Be(1);
                summary.GrossAmountTotal.Should().Be(300m);
                summary.RemainingBudget.Should().Be(700m);
            });
        }
    }
}
