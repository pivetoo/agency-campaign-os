using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;

namespace AgencyCampaign.Api.BackgroundJobs
{
    /// <summary>
    /// Rotina periodica que alerta o responsavel quando uma oportunidade aberta fica parada num estagio
    /// alem do SLA do estagio (deal rotting). Roda por tenant; a deduplicacao vive na entidade
    /// (StaleAlertedAt), resetada ao mudar de estagio, entao gera um alerta por entrada no estagio.
    /// </summary>
    public sealed class OpportunityStalledJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan TickInterval = TimeSpan.FromHours(12);

        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<OpportunityStalledJob> logger;

        public OpportunityStalledJob(TenantJobRunner tenantJobRunner, ILogger<OpportunityStalledJob> logger)
        {
            this.tenantJobRunner = tenantJobRunner;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using PeriodicTimer timer = new(TickInterval);

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
                    IOpportunityService opportunities = provider.GetRequiredService<IOpportunityService>();
                    int alerted = await opportunities.AlertStalled(cancellationToken);

                    if (alerted > 0)
                    {
                        logger.LogInformation("OpportunityStalledJob: {Alerted} oportunidades paradas alertadas.", alerted);
                    }
                }, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "OpportunityStalledJob run failed.");
            }
        }
    }
}
