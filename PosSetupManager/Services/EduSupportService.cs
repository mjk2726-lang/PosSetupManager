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
                await _page.ClickAsync("#creteAppletDoc", new PageClickOptions { Timeout = 8000 });
                await _page.WaitForTimeoutAsync(1000);

                // 매장명 검색 버튼
                await _page.WaitForSelectorAsync("a.btn_minor_s", new PageWaitForSelectorOptions { Timeout = 10000 });
                await _page.ClickAsync("a.btn_minor_s", new PageClickOptions { Timeout = 5000 });
                await _page.WaitForTimeoutAsync(800);

                // 팝업 내 검색어 입력
                await _page.WaitForSelectorAsync("#searchKeyword", new PageWaitForSelectorOptions { Timeout = 8000 });
                await _page.FillAsync("#searchKeyword", storeName);

                // 팝업 내 검색 버튼 클릭
                await _page.ClickAsync("#searchBtn", new PageClickOptions { Timeout = 5000 });
                await _page.WaitForTimeoutAsync(1000);

                // 결과에서 매장명 클릭
                var result = _page.Locator("td").Filter(new LocatorFilterOptions { HasText = storeName });
                await result.First.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 8000 });
                await _page.WaitForTimeoutAsync(500);

                // 팝업 닫기
                await _page.ClickAsync("a[title='닫기']", new PageClickOptions { Timeout = 5000 });
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
