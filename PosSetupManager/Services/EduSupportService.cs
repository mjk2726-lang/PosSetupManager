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

                // 검색 결과 확인
                var result = _page.Locator("td").Filter(new LocatorFilterOptions { HasText = storeName });
                int count = await result.CountAsync();
                if (count == 0)
                    return Tuple.Create(false, "검색결과가 없습니다. 직접등록해주세요.");

                // 결과 첫 번째 클릭
                await result.First.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 8000 });
                await _page.WaitForTimeoutAsync(500);

                // 팝업 닫기
                await _page.ClickAsync("a.btn_minor_s[title='닫기']", new PageClickOptions { Timeout = 5000 });
                await _page.WaitForTimeoutAsync(800);

                // 확인 버튼
                await _page.ClickAsync("a.btn_major.btn-confirm", new PageClickOptions { Timeout = 8000 });

                // 등록 성공 여부 확인 (URL이 /doc/new → /doc/{id} 로 변경됨)
                await _page.WaitForURLAsync(
                    url => url.Contains("/applet/32498/doc/") && !url.Contains("/new"),
                    new PageWaitForURLOptions { Timeout = 15000 });

                return Tuple.Create(true, (string)null);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, "교육지원 등록 실패: " + ex.Message);
            }
            finally
            {
                try { await _page.CloseAsync(); } catch { }
            }
        }
    }
}
