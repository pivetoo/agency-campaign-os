using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;

namespace AgencyCampaign.Api.BackgroundJobs
{
    /// <summary>
    /// Rotina periodica que lembra (agency-wide) os entregaveis com prazo vencido ou a vencer dentro da
    /// janela. Roda por tenant; a deduplicacao vive na entidade (DeadlineReminderSentAt), resetada quando
    /// o prazo e remarcado - um lembrete por prazo.
    /// </summary>
    public sealed class DeliverableDeadlineJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan TickInterval = TimeSpan.FromHours(6);
        private const int WindowDays = 3;

        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<DeliverableDeadlineJob> logger;

        public DeliverableDeadlineJob(TenantJobRunner tenantJobRunner, ILogger<DeliverableDeadlineJob> logger)
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
                    ICampaignDeliverableService deliverables = provider.GetRequiredService<ICampaignDeliverableService>();
                    int reminded = await deliverables.RemindDueDeliverables(WindowDays, cancellationToken);

                    if (reminded > 0)
                    {
                        logger.LogInformation("DeliverableDeadlineJob: {Reminded} entregaveis lembrados.", reminded);
                    }
                }, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "DeliverableDeadlineJob run failed.");
            }
        }
    }
}
