using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PosSetupManager.Services
{
    public class BrowserService
    {
        // 세션 데이터: EXE 옆 BrowserProfile 폴더
        public static readonly string UserDataDir = Path.Combine(
            Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath),
            "BrowserProfile");

        private static IPlaywright _playwright;
        private static IBrowserContext _context;

        public static IBrowserContext Context => _context;

        public static async Task EnsureLaunchedAsync()
        {
            if (_context != null)
            {
                try
                {
                    var testPage = await _context.NewPageAsync();
                    await testPage.CloseAsync();
                    return;
                }
                catch
                {
                    _context = null;
                    try { _playwright?.Dispose(); } catch { }
                    _playwright = null;
                }
            }

            // EXE 옆 .playwright 폴더를 Playwright 드라이버로 지정
            string exeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            string localNodePath = Path.Combine(exeDir, ".playwright", "node", "win32_x64", "node.exe");

            if (File.Exists(localNodePath))
            {
                // EXE 옆 .playwright 폴더 사용
                Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", localNodePath);
                Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_PATH",
                    Path.Combine(exeDir, ".playwright", "package", "cli.js"));
            }

            _playwright = await Playwright.CreateAsync();

            var options = new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                Channel = "chrome",
                Locale = "ko-KR",
                ViewportSize = null,
                Args = new[] { "--start-maximized", "--no-restore-session-state" }
            };

            _context = await _playwright.Chromium.LaunchPersistentContextAsync(UserDataDir, options);
        }

        public static async Task<IPage> NewPageAsync()
        {
            await EnsureLaunchedAsync();
            var page = await _context.NewPageAsync();
            await page.BringToFrontAsync();
            return page;
        }
    }
}