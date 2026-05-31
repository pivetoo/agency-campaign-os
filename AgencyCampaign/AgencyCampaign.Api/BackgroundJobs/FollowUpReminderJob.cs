using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;

namespace AgencyCampaign.Api.BackgroundJobs
{
    /// <summary>
    /// Rotina periodica que lembra os responsaveis de follow-ups vencidos (ou que vencem hoje) de
    /// oportunidades abertas. Roda por tenant; a deduplicacao vive na entidade (ReminderSentAt), entao
    /// cada follow-up so gera um lembrete por vencimento (reset ao remarcar a data).
    /// </summary>
    public sealed class FollowUpReminderJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan TickInterval = TimeSpan.FromHours(6);

        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<FollowUpReminderJob> logger;

        public FollowUpReminderJob(TenantJobRunner tenantJobRunner, ILogger<FollowUpReminderJob> logger)
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
                    IOpportunityFollowUpService followUps = provider.GetRequiredService<IOpportunityFollowUpService>();
                    int reminded = await followUps.RemindDue(cancellationToken);

                    if (reminded > 0)
                    {
                        logger.LogInformation("FollowUpReminderJob: {Reminded} follow-ups lembrados.", reminded);
                    }
                }, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "FollowUpReminderJob run failed.");
            }
        }
    }
}
