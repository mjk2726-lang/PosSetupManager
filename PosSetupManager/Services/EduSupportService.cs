using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PosSetupManager.Services
{
    public class EduSupportService
    {
        private const string TARGET_URL = "https://sorder1004.daouoffice.com/gw/app/works/applet/32498/home";
        private readonly IPage _page;

        public EduSupportService(IPage page)
        {
            _page = page;
        }

        public async Task<Tuple<bool, string>> RegisterAsync(string storeName)
        {
            try
            {
                await _page.GotoAsync(TARGET_URL, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });
                await _page.WaitForTimeoutAsync(1500);

                // 등록 버튼
                await _page.ClickAsync("a.btn_write", new PageClickOptions { Timeout = 8000 });
                await _page.WaitForTimeoutAsync(1000);

                // 매장명 검색 버튼
                await _page.WaitForSelectorAsync("button:has-text('검색')", new PageWaitForSelectorOptions { Timeout = 10000 });
                await _page.ClickAsync("button:has-text('검색')", new PageClickOptions { Timeout = 5000 });
                await _page.WaitForTimeoutAsync(800);

                // 팝업 내 검색어 입력
                var popupInput = _page.Locator("[class*='layer'] input[type='text'], [class*='popup'] input[type='text'], [class*='modal'] input[type='text']").First;
                await popupInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 8000 });
                await popupInput.FillAsync(storeName);

                // 팝업 내 검색 버튼 클릭
                await _page.ClickAsync("[class*='layer'] button:has-text('검색'), [class*='popup'] button:has-text('검색'), [class*='modal'] button:has-text('검색')", new PageClickOptions { Timeout = 5000 });
                await _page.WaitForTimeoutAsync(1000);

                // 결과에서 매장명 클릭
                var result = _page.Locator("td").Filter(new LocatorFilterOptions { HasText = storeName });
                await result.First.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 8000 });
                await _page.WaitForTimeoutAsync(800);

                // 확인 버튼
                await _page.ClickAsync("a.btn_major.btn-confirm", new PageClickOptions { Timeout = 8000 });
                await _page.WaitForTimeoutAsync(2000);

                return Tuple.Create(true, (string)null);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, "교육지원 등록 실패: " + ex.Message);
            }
        }
    }
}
