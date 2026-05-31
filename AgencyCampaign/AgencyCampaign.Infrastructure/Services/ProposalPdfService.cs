using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalPdfService : IProposalPdfService
    {
        private readonly DbContext dbContext;

        public ProposalPdfService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<byte[]> GenerateForProposalAsync(long proposalId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await LoadProposalAsync(proposalId, cancellationToken);
            if (proposal is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            AgencySettings agency = await ResolveAgencyAsync(cancellationToken);
            string? template = await ResolveTemplateAsync(proposal, agency, cancellationToken);
            string html = ProposalHtmlBuilder.Build(proposal, agency, template);
            return await RenderToPdfAsync(html);
        }

        public async Task<byte[]?> GenerateForShareTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            ProposalShareLink? shareLink = await dbContext.Set<ProposalShareLink>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Token == token, cancellationToken);

            if (shareLink is null || !shareLink.IsActive(DateTimeOffset.UtcNow))
            {
                return null;
            }

            Proposal? proposal = await LoadProposalAsync(shareLink.ProposalId, cancellationToken);
            if (proposal is null)
            {
                return null;
            }

            AgencySettings agency = await ResolveAgencyAsync(cancellationToken);
            string? template = await ResolveTemplateAsync(proposal, agency, cancellationToken);
            string html = ProposalHtmlBuilder.Build(proposal, agency, template);
            return await RenderToPdfAsync(html);
        }

        private async Task<Proposal?> LoadProposalAsync(long proposalId, CancellationToken cancellationToken)
        {
            return await dbContext.Set<Proposal>()
                .AsNoTracking()
                .Include(item => item.Opportunity)
                    .ThenInclude(item => item!.Brand)
                .Include(item => item.Items)
                    .ThenInclude(item => item.Creator)
                .Include(item => item.ProposalLayout)
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);
        }

        private async Task<string?> ResolveTemplateAsync(Proposal proposal, AgencySettings agency, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(proposal.ProposalLayout?.Template))
            {
                return proposal.ProposalLayout.Template;
            }

            ProposalTemplateVersion? defaultLayout = await dbContext.Set<ProposalTemplateVersion>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(defaultLayout?.Template))
            {
                return defaultLayout.Template;
            }

            return string.IsNullOrWhiteSpace(agency.ProposalHtmlTemplate) ? null : agency.ProposalHtmlTemplate;
        }

        private async Task<AgencySettings> ResolveAgencyAsync(CancellationToken cancellationToken)
        {
            return await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? new AgencySettings("Minha agência");
        }

        private static readonly SemaphoreSlim BrowserInitLock = new(1, 1);
        private static readonly SemaphoreSlim RenderConcurrency = new(3, 3);
        private const int RenderTimeoutMs = 30_000;
        private static IBrowser? sharedBrowser;

        // Reusa um unico navegador entre requisicoes (relanca se cair), em vez de subir e
        // descartar um Chromium por PDF. Protege memoria/CPU do servidor sob acessos simultaneos.
        private static async Task<IBrowser> GetBrowserAsync()
        {
            if (sharedBrowser is { IsConnected: true })
            {
                return sharedBrowser;
            }

            await BrowserInitLock.WaitAsync();
            try
            {
                if (sharedBrowser is { IsConnected: true })
                {
                    return sharedBrowser;
                }

                if (sharedBrowser is not null)
                {
                    try
                    {
                        await sharedBrowser.DisposeAsync();
                    }
                    catch
                    {
                        // navegador ja morto; segue para relancar
                    }
                }

                sharedBrowser = await Puppeteer.LaunchAsync(BuildLaunchOptions());
                return sharedBrowser;
            }
            finally
            {
                BrowserInitLock.Release();
            }
        }

        private static async Task<byte[]> RenderToPdfAsync(string html)
        {
            await RenderConcurrency.WaitAsync();
            try
            {
                IBrowser browser = await GetBrowserAsync();
                await using IPage page = await browser.NewPageAsync();
                page.DefaultTimeout = RenderTimeoutMs;

                await page.SetContentAsync(html, new NavigationOptions
                {
                    WaitUntil = [WaitUntilNavigation.Load],
                    Timeout = RenderTimeoutMs
                });

                return await page.PdfDataAsync(new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true,
                    MarginOptions = new MarginOptions
                    {
                        Top = "0",
                        Bottom = "0",
                        Left = "0",
                        Right = "0"
                    }
                });
            }
            finally
            {
                RenderConcurrency.Release();
            }
        }

        private static LaunchOptions BuildLaunchOptions()
        {
            string[] sandboxArgs = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu"];

            string? executablePath = Environment.GetEnvironmentVariable("CHROMIUM_EXECUTABLE_PATH");

            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                return new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = executablePath,
                    Args = sandboxArgs
                };
            }

            return new LaunchOptions
            {
                Headless = true,
                Args = sandboxArgs
            };
        }
    }
}
