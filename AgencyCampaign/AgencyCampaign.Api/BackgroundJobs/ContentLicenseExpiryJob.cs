using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.BackgroundJobs;
using AgencyCampaign.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.BackgroundJobs
{
    /// <summary>
    /// Rotina de alerta de vencimento de direitos de uso. Faz um tick periodico (diario por padrao)
    /// e, por tenant, alerta as licencas proximas do vencimento. A deduplicacao por threshold vive
    /// no servico (LastAlertedThresholdDays), o que torna a rotina restart-safe sem persistir agendamento.
    /// </summary>
    public sealed class ContentLicenseExpiryJob : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(3);

        private readonly TenantJobRunner tenantJobRunner;
        private readonly ContentLicenseOptions options;
        private readonly ILogger<ContentLicenseExpiryJob> logger;

        public ContentLicenseExpiryJob(TenantJobRunner tenantJobRunner, IOptions<ContentLicenseOptions> options, ILogger<ContentLicenseExpiryJob> logger)
        {
            this.tenantJobRunner = tenantJobRunner;
            this.options = options.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.JobEnabled)
            {
                logger.LogInformation("ContentLicenseExpiryJob disabled via configuration.");
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
                    IContentLicenseService licenses = provider.GetRequiredService<IContentLicenseService>();
                    int alerted = await licenses.AlertExpiring(options.AlertThresholdsDays, cancellationToken);

                    if (alerted > 0)
                    {
                        logger.LogInformation("ContentLicenseExpiryJob: {Alerted} licencas alertadas.", alerted);
                    }
                }, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "ContentLicenseExpiryJob run failed.");
            }
        }
    }
}
