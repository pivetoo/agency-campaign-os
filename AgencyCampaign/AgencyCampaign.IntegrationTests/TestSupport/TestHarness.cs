using AgencyCampaign.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AgencyCampaign.IntegrationTests
{
    // Sobe um Postgres efemero (Testcontainers) UMA vez para toda a suite, roda as migrations reais
    // do AgencyCampaign (+ Archon base) num banco LIMPO e expoe um ServiceProvider com a Infrastructure
    // real. Com um unico tenant configurado, o ResolveCurrentTenant do Archon resolve o DbContext
    // automaticamente (single-tenant, sem fallback silencioso).
    //
    // Mora em TestSupport/ mas permanece no namespace RAIZ de proposito: um [SetUpFixture] cobre seu
    // namespace e os descendentes, entao no raiz ele vale para todos os fixtures em
    // AgencyCampaign.IntegrationTests.* (Crud, Financial, etc.). Se descesse para um subnamespace, o
    // container nao subiria para os testes das outras pastas.
    //
    // Requer Docker no host (disponivel nos runners do GitHub Actions; nao roda em sandbox sem Docker).
    [SetUpFixture]
    public sealed class TestHarness
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public const string DbName = "agencyintegrationtests";
        public const string DbUser = "testuser";
        public const string DbPass = "testpass";
        private static PostgreSqlContainer? container;

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase(DbName)
                .WithUsername(DbUser)
                .WithPassword(DbPass)
                .Build();

            await container.StartAsync();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RunMigrations"] = "true",
                    ["TenantDatabases:test:ConnectionString"] = container.GetConnectionString(),
                    ["TenantDatabases:test:DatabaseType"] = "PostgreSql",
                    ["TenantDatabases:test:Schema"] = "public",
                })
                .Build();

            ServiceCollection services = new();
            services.AddLogging();
            // IStringLocalizer e usado por varios servicos; no app real vem do AddArchonApi.
            services.AddLocalization();
            // Roda as migrations contra o container e registra persistencia + servicos reais.
            services.AddAgencyCampaignInfrastructure(configuration);

            Services = services.BuildServiceProvider();
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            if (Services is IAsyncDisposable disposableProvider)
            {
                await disposableProvider.DisposeAsync();
            }

            if (container is not null)
            {
                await container.DisposeAsync();
            }
        }
    }
}
