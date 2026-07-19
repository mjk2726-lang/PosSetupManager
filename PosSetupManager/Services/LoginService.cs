using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PosSetupManager.Services
{
    public class LoginService
    {
        private const string TARGET_URL = "https://sorder1004.daouoffice.com/gw/app/works/applet/21391/doc/new";

        public async Task<Tuple<bool, string>> EnsureLoggedInAsync(IPage page)
        {
            await page.GotoAsync(TARGET_URL, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            if (!IsLoginPage(page.Url))
                return Tuple.Create(true, (string)null);

            // 로그인 필요
            if (!Forms.CredentialStore.Load(out string id, out string pw))
                return Tuple.Create(false, "저장된 로그인 정보가 없습니다.\n설정 탭에서 로그인 정보를 입력해주세요.");

            try
            {
                await page.FillAsync("input.input_txt[type='text']", id, new PageFillOptions { Timeout = 5000 });
                await page.FillAsync("input[type='password']", pw, new PageFillOptions { Timeout = 5000 });
                await page.ClickAsync("button[type='submit']", new PageClickOptions { Timeout = 5000 });

                await page.WaitForURLAsync(
                    url => !IsLoginPage(url),
                    new PageWaitForURLOptions { Timeout = 15000 });
            }
            catch
            {
                return Tuple.Create(false, "로그인에 실패했습니다.\n아이디/비밀번호를 확인하고 설정에서 다시 저장해주세요.");
            }

            // 로그인 후 목표 URL 이동
            await page.GotoAsync(TARGET_URL, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            return Tuple.Create(true, (string)null);
        }

        private bool IsLoginPage(string url)
        {
            return url.Contains("login") || url.Contains("signin");
        }
    }
}