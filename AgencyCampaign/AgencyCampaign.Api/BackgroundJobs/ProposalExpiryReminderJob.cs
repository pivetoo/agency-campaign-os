using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;

namespace AgencyCampaign.Api.BackgroundJobs
{
    /// <summary>
    /// Rotina periodica que avisa o responsavel quando uma proposta enviada/visualizada esta perto de
    /// expirar (dentro da janela de dias configurada), para que ele faca follow-up com o cliente ou
    /// estenda a validade antes de perder o deal. Roda por tenant; deduplicacao na entidade
    /// (ExpiryReminderSentAt), resetada ao reenviar -> um lembrete por ciclo de envio.
    /// </summary>
    public sealed class ProposalExpiryReminderJob : BackgroundService
    {
        private const int DaysAhead = 3;
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan TickInterval = TimeSpan.FromHours(12);

        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<ProposalExpiryReminderJob> logger;

        public ProposalExpiryReminderJob(TenantJobRunner tenantJobRunner, ILogger<ProposalExpiryReminderJob> logger)
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
                    IProposalService proposals = provider.GetRequiredService<IProposalService>();
                    int reminded = await proposals.RemindExpiringSoon(DaysAhead, cancellationToken);

                    if (reminded > 0)
                    {
                        logger.LogInformation("ProposalExpiryReminderJob: {Reminded} propostas lembradas.", reminded);
                    }
                }, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "ProposalExpiryReminderJob run failed.");
            }
        }
    }
}
