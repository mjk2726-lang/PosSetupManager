using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PosSetupManager.Services
{
    public class BrowserService
    {
        public static readonly string UserDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSetupManager", "BrowserProfile");

        // ── 싱글턴 ──
        private static IPlaywright _playwright;
        private static IBrowserContext _context;

        public static IBrowserContext Context => _context;

        // 브라우저가 이미 실행 중이면 재사용, 아니면 새로 실행
        public static async Task EnsureLaunchedAsync()
        {
            if (_context != null)
            {
                try
                {
                    // 실제로 살아있는지 새 페이지 생성으로 확인
                    var testPage = await _context.NewPageAsync();
                    await testPage.CloseAsync();
                    return;
                }
                catch
                {
                    // 죽어있으면 초기화
                    _context = null;
                    try { _playwright?.Dispose(); } catch { }
                    _playwright = null;
                }
            }

            _playwright = await Playwright.CreateAsync();
            var options = new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                Channel = "chrome",
                Locale = "ko-KR",
                ViewportSize = null,
                Args = new[] { "--start-maximized" }
            };
            _context = await _playwright.Chromium.LaunchPersistentContextAsync(UserDataDir, options);
        }

        // 매장마다 새 탭 생성
        public static async Task<IPage> NewPageAsync()
        {
            await EnsureLaunchedAsync();
            var page = await _context.NewPageAsync();
            await page.BringToFrontAsync();
            return page;
        }
    }
}