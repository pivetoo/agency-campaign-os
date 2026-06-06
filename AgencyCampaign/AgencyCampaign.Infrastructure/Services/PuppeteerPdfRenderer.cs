using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace AgencyCampaign.Infrastructure.Services
{
    // Renderizador PDF compartilhado: um unico Chromium reusado entre propostas e relatorios.
    public static class PuppeteerPdfRenderer
    {
        private static readonly SemaphoreSlim BrowserInitLock = new(1, 1);
        private static readonly SemaphoreSlim RenderConcurrency = new(3, 3);
        private const int RenderTimeoutMs = 30_000;
        private static IBrowser? sharedBrowser;

        public static async Task<byte[]> RenderToPdfAsync(string html)
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
                    MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" }
                });
            }
            finally
            {
                RenderConcurrency.Release();
            }
        }

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

        private static LaunchOptions BuildLaunchOptions()
        {
            string[] sandboxArgs = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu"];
            string? executablePath = Environment.GetEnvironmentVariable("CHROMIUM_EXECUTABLE_PATH");

            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                return new LaunchOptions { Headless = true, ExecutablePath = executablePath, Args = sandboxArgs };
            }

            return new LaunchOptions { Headless = true, Args = sandboxArgs };
        }
    }
}
