using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalPdfService : IProposalPdfService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalPdfService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<byte[]> GenerateForProposalAsync(long proposalId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await LoadProposalAsync(proposalId, cancellationToken);
            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            AgencySettings agency = await ResolveAgencyAsync(cancellationToken);
            string html = ProposalHtmlBuilder.Build(proposal, agency);
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
            string html = ProposalHtmlBuilder.Build(proposal, agency);
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
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);
        }

        private async Task<AgencySettings> ResolveAgencyAsync(CancellationToken cancellationToken)
        {
            return await dbContext.Set<AgencySettings>()
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? new AgencySettings("Minha agência");
        }

        private static async Task<byte[]> RenderToPdfAsync(string html)
        {
            LaunchOptions options = BuildLaunchOptions();

            await using IBrowser browser = await Puppeteer.LaunchAsync(options);
            await using IPage page = await browser.NewPageAsync();

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Load]
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
