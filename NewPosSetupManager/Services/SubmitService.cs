using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace NewPosSetupManager.Services
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

                // 등록 버튼 후보: btn-confirm 또는 btn_major
                ILocator btn = null;
                var btnConfirm = _page.Locator("a.btn_major.btn-confirm");
                var btnMajor = _page.Locator("a.btn_major");

                if (await btnConfirm.CountAsync() > 0)
                    btn = btnConfirm;
                else if (await btnMajor.CountAsync() > 0)
                    btn = btnMajor.First;

                if (btn == null)
                    return Tuple.Create(false, "등록 버튼을 찾을 수 없습니다.");

                await btn.ScrollIntoViewIfNeededAsync();
                await btn.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 10000 });

                // 등록 완료: URL 변경 확인
                try
                {
                    await _page.WaitForURLAsync(
                        url => url != urlBefore,
                        new PageWaitForURLOptions { Timeout = 15000 });
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
