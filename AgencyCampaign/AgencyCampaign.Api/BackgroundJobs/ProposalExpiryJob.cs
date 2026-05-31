using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;

namespace AgencyCampaign.Api.BackgroundJobs
{
    /// <summary>
    /// Rotina periodica que marca como expiradas as propostas enviadas cuja validade ja venceu.
    /// Roda por tenant via TenantJobRunner; a logica e idempotente (so altera propostas Sent vencidas).
    /// </summary>
    public sealed class ProposalExpiryJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan TickInterval = TimeSpan.FromHours(12);

        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<ProposalExpiryJob> logger;

        public ProposalExpiryJob(TenantJobRunner tenantJobRunner, ILogger<ProposalExpiryJob> logger)
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
                    int expired = await proposals.ExpireOverdue(cancellationToken);

                    if (expired > 0)
                    {
                        logger.LogInformation("ProposalExpiryJob: {Expired} propostas expiradas.", expired);
                    }
                }, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "ProposalExpiryJob run failed.");
            }
        }
    }
}
