using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;
using AgencyCampaign.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.BackgroundJobs
{
    /// <summary>
    /// Rotina de coleta automatica via Apify. Faz um tick periodico (diario por padrao) e,
    /// por tenant, dispara o sync de posts e de seguidores. A cadencia real (posts semanal,
    /// seguidores mensal) e garantida pelos cooldowns dos servicos, nao pelo intervalo do tick,
    /// o que torna a rotina restart-safe sem persistir estado de agendamento.
    /// </summary>
    public sealed class SocialSyncJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

        private readonly TenantJobRunner tenantJobRunner;
        private readonly ApifyOptions options;
        private readonly ILogger<SocialSyncJob> logger;

        public SocialSyncJob(TenantJobRunner tenantJobRunner, IOptions<ApifyOptions> options, ILogger<SocialSyncJob> logger)
        {
            this.tenantJobRunner = tenantJobRunner;
            this.options = options.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.JobEnabled)
            {
                logger.LogInformation("SocialSyncJob disabled via configuration.");
                return;
            }

            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using PeriodicTimer timer = new(TimeSpan.FromHours(Math.Max(1, options.JobTickHours)));

            do
            {
                await RunOnce(stoppingToken);
            }
            while (await WaitForNextTick(timer, stoppingToken));
        }

        private static async Task<bool> WaitForNextTick(PeriodicTimer timer, CancellationToken stoppingToken)
        {
            try
            {
                return await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async Task RunOnce(CancellationToken stoppingToken)
        {
            try
            {
                await tenantJobRunner.RunForAllTenants(async (provider, cancellationToken) =>
                {
                    IDeliverableMetricsSyncService posts = provider.GetRequiredService<IDeliverableMetricsSyncService>();
                    int postsSynced = await posts.SyncAll(TimeSpan.FromDays(options.PostSyncCooldownDays), cancellationToken);

                    ICreatorAudienceSyncService followers = provider.GetRequiredService<ICreatorAudienceSyncService>();
                    int followersSynced = await followers.SyncAll(TimeSpan.FromDays(options.FollowerSyncCooldownDays), cancellationToken);

                    if (postsSynced > 0 || followersSynced > 0)
                    {
                        logger.LogInformation("SocialSyncJob: {Posts} posts e {Followers} handles sincronizados.", postsSynced, followersSynced);
                    }
                }, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "SocialSyncJob run failed.");
            }
        }
    }
}
