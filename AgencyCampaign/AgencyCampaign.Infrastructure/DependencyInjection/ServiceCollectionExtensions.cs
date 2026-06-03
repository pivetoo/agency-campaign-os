using AgencyCampaign.Application.Abstractions;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;
using AgencyCampaign.Infrastructure.Clients;
using AgencyCampaign.Infrastructure.Options;
using AgencyCampaign.Infrastructure.Security;
using AgencyCampaign.Infrastructure.Services;
using Archon.Infrastructure.DependencyInjection;
using Archon.Infrastructure.Migrations;
using Archon.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgencyCampaign.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAgencyCampaignInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DocumentEmailOptions>(configuration.GetSection("DocumentEmail"));
            services.Configure<WebhookOptions>(configuration.GetSection("Webhooks"));
            services.Configure<ApifyOptions>(configuration.GetSection("Apify"));
            services.Configure<ContentLicenseOptions>(configuration.GetSection("ContentLicense"));
            services.Configure<MediaStorageOptions>(configuration.GetSection("MediaStorage"));
            services.AddArchonPersistence(configuration, typeof(ServiceCollectionExtensions).Assembly);
            services.RunMigrations(
                configuration,
                GetMigrationSchema(configuration),
                typeof(DatabaseMigrator).Assembly,
                typeof(ServiceCollectionExtensions).Assembly);
            services.AddServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
            services.AddArchonRestApi();
            services.AddScoped<IntegrationPlatformClient>();
            services.AddHttpClient<IApifySocialMetricsClient, ApifySocialMetricsClient>();
            services.AddHttpClient<ISignedDocumentDownloader, SignedDocumentDownloader>();
            services.AddSingleton<TenantJobRunner>();
            services.AddScoped<IAutomationDispatcher, AutomationDispatcher>();
            services.AddScoped<IFinancialAutoGeneration, FinancialAutoGenerationService>();
            services.AddScoped<IImageUploadStorage, ImageUploadStorage>();
            services.AddScoped<IContentFileStorage, ContentFileStorage>();
            services.AddScoped<IMediaAccessTokenService, MediaAccessTokenService>();
            services.AddHttpContextAccessor();
            services.AddScoped<IPermissionChecker, PermissionChecker>();

            return services;
        }

        private static string GetMigrationSchema(IConfiguration configuration)
        {
            TenantDatabaseOptions tenantDatabaseOptions = new();
            configuration.Bind(tenantDatabaseOptions);

            string? schema = tenantDatabaseOptions.TenantDatabases
                .Select(item => item.Value.Schema)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            return schema ?? "public";
        }
    }
}
