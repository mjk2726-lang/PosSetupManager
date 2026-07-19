using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PosSetupManager.Services
{
    public class SubmitService
    {
        private readonly IPage _page;

        public SubmitService(IPage page)
        {
            _page = page;
        }

        public async Task<Tuple<bool, string>> SubmitAsync()
        {
            try
            {
                var urlBefore = _page.Url;

                // 버튼 위치를 찾아서 마우스로 직접 클릭
                var btn = _page.Locator("a.btn_major.btn-confirm");
                await btn.ScrollIntoViewIfNeededAsync();
                await btn.ClickAsync(new LocatorClickOptions
                {
                    Force = true,
                    Timeout = 5000
                });

                // 등록 완료 시 URL 변경 확인
                try
                {
                    await _page.WaitForURLAsync(
                        url => !url.Contains("/doc/new") && url != urlBefore,
                        new PageWaitForURLOptions { Timeout = 10000 });
                    return Tuple.Create(true, (string)null);
                }
                catch
                {
                    return Tuple.Create(false, "등록이 완료되지 않았습니다.\n다우오피스 화면을 확인해주세요.");
                }
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, "등록 버튼 클릭 실패:\n" + ex.Message);
            }
        }
    }
}