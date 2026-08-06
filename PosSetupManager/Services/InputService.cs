using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using PosSetupManager.Models;

namespace PosSetupManager.Services
{
    public class InputService
    {
        private readonly IPage _page;

        public InputService(IPage page)
        {
            _page = page;
        }

        // ── 텍스트 ──
        public async Task FillText(string cid, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var sel = string.Format("[data-cid='{0}'] input[type='text'], [data-cid='{0}'] input.txt", cid);
            try { await _page.FillAsync(sel, value, new PageFillOptions { Timeout = 3000 }); }
            catch { }
        }

        public async Task FillTextArea(string cid, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var sel = string.Format("[data-cid='{0}'] textarea", cid);
            try { await _page.FillAsync(sel, value, new PageFillOptions { Timeout = 3000 }); }
            catch { }
        }

        public async Task FillLocalMode(bool menuBoard, bool noticeBoard)
        {
            if (menuBoard)
            {
                var sel = "[data-cid='_ia3qcbzea'] label[for='_ia3qcbzea_0']";
                try { await _page.ClickAsync(sel, new PageClickOptions { Timeout = 3000 }); } catch { }
            }
            if (noticeBoard)
            {
                var sel = "[data-cid='_ia3qcbzea'] label[for='_ia3qcbzea_1']";
                try { await _page.ClickAsync(sel, new PageClickOptions { Timeout = 3000 }); } catch { }
            }
        }

        public async Task<string> GetValue(string cid, string childSelector)
        {
            try
            {
                var js = string.Format(
                    "document.querySelector('[data-cid=\"{0}\"] {1}')?.value ?? 'null'",
                    cid, childSelector);
                var result = await _page.EvaluateAsync<string>(js);
                return result;
            }
            catch (Exception ex) { return "error: " + ex.Message; }
        }

        // ── 날짜 + 시간 ──
        public async Task FillDate(string cid, DateTime date, string time = "")
        {
            try
            {
                string formattedTime = "";
                if (!string.IsNullOrEmpty(time))
                {
                    if (time.Contains(":"))
                    {
                        var parts = time.Split(':');
                        formattedTime = string.Format("{0:D2}:{1:D2}",
                            int.Parse(parts[0]),
                            parts.Length > 1 ? int.Parse(parts[1]) : 0);
                    }
                    else
                    {
                        formattedTime = string.Format("{0:D2}:00", int.Parse(time));
                    }
                }
                var js = string.Format(@"
                    (function() {{
                        var dateEl = document.querySelector('[data-cid=""{0}""] input[data-date=""start""]');
                        if(dateEl) {{
                            var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                            setter.call(dateEl, '{1}');
                            dateEl.dispatchEvent(new Event('input', {{ bubbles: true }}));
                            dateEl.dispatchEvent(new Event('change', {{ bubbles: true }}));
                        }}
                        var timeEl = document.querySelector('[data-cid=""{0}""] input[data-time=""start""]');
                        if(timeEl && '{2}' !== '') {{
                            timeEl.value = '{2}';
                            timeEl.dispatchEvent(new Event('input', {{ bubbles: true }}));
                            timeEl.dispatchEvent(new Event('change', {{ bubbles: true }}));
                        }}
                    }})();
                ", cid, date.ToString("yyyy-MM-dd"), formattedTime);
                await _page.EvaluateAsync(js);
                await _page.WaitForTimeoutAsync(300);

                if (!string.IsNullOrEmpty(formattedTime))
                {
                    var timeJs = string.Format(@"
                        var timeEl = document.querySelector('[data-cid=""{0}""] input[data-time=""start""]');
                        if(timeEl) {{ timeEl.value = '{1}'; }}
                    ", cid, formattedTime);
                    await _page.EvaluateAsync(timeJs);
                }
            }
            catch { }
        }

        // ── 시간 ──
        public async Task FillTime(string cid, string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) return;
            var sel = string.Format("[data-cid='{0}'] input[data-time='start']", cid);
            try { await _page.FillAsync(sel, timeStr, new PageFillOptions { Timeout = 3000 }); }
            catch { }
        }

        // ── 라디오/O·X ──
        public async Task ClickByLabel(string cid, string labelText)
        {
            if (string.IsNullOrEmpty(labelText)) return;
            var sel = string.Format("[data-cid='{0}'] label", cid);
            try
            {
                var labels = _page.Locator(sel);
                int count = await labels.CountAsync();
                for (int i = 0; i < count; i++)
                {
                    var lbl = labels.Nth(i);
                    string txt = (await lbl.InnerTextAsync()).Trim();
                    if (txt == labelText)
                    {
                        await lbl.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                        break;
                    }
                }
            }
            catch { }
        }

        public async Task FillOx(string cid, string ox)
        {
            if (string.IsNullOrEmpty(ox)) return;
            await ClickByLabel(cid, ox);
        }

        // ── 드롭다운 ──
        public async Task FillSelect(string cid, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var sel = string.Format("[data-cid='{0}'] select", cid);
            try
            {
                await _page.SelectOptionAsync(sel,
                    new SelectOptionValue { Label = value },
                    new PageSelectOptionOptions { Timeout = 3000 });
            }
            catch { }
        }

        // ── POS 종류 체크박스 ──
        public async Task FillPosTypes(List<string> posTypes)
        {
            // 실제 다우오피스 input id 매핑 (id가 순서와 다름)
            var posIdMap = new System.Collections.Generic.Dictionary<string, string>
            {
                {"이지포스",           "_usvoeowq9_0"},
                {"엠포스",             "_usvoeowq9_1"},
                {"스마일포스",         "_usvoeowq9_2"},
                {"오케이포스",         "_usvoeowq9_3"},
                {"메이트포스",         "_usvoeowq9_4"},
                {"배달포스",           "_usvoeowq9_11"},
                {"K포스",              "_usvoeowq9_12"},
                {"하이픈포스(개통불가)","_usvoeowq9_9"},
                {"유니온포스",         "_usvoeowq9_10"},
                {"키움포스",           "_usvoeowq9_13"},
                {"팝스(웨이브포스)",   "_usvoeowq9_14"},
                {"링크포스",           "_usvoeowq9_15"},
                {"포스마스터(티페이)", "_usvoeowq9_16"},
                {"퍼스트포스",         "_usvoeowq9_18"},
                {"에어포스",           "_usvoeowq9_19"},
                {"토스포스",           "_usvoeowq9_20"},
                {"포스메이커스(개통불가)","_usvoeowq9_21"},
                {"윙스포스",           "_usvoeowq9_23"},
                {"안시포스",           "_usvoeowq9_22"},
                {"기타",               "_usvoeowq9_5"},
                {"연동안함",           "_usvoeowq9_6"},
                {"타밴",               "_usvoeowq9_7"},
                {"우리밴",             "_usvoeowq9_8"},
            };

            var snapshot = new System.Collections.Generic.List<string>(posTypes);
            foreach (var posType in snapshot)
            {
                if (posIdMap.ContainsKey(posType))
                {
                    var sel = string.Format("label[for='{0}']", posIdMap[posType]);
                    try { await _page.ClickAsync(sel, new PageClickOptions { Timeout = 3000 }); }
                    catch { }
                }
            }
        }

        // ── 선불 체크박스 ──
        public async Task FillPrepaid(bool table, bool payment, bool over5Man, bool ksnet)
        {
            if (table) await SafeClick("[data-cid='_h28myd6ub'] label[for='_h28myd6ub_0']");
            if (payment) await SafeClick("[data-cid='_h28myd6ub'] label[for='_h28myd6ub_1']");
            if (over5Man) await SafeClick("[data-cid='_h28myd6ub'] label[for='_h28myd6ub_3']");
            if (ksnet) await SafeClick("[data-cid='_h28myd6ub'] label[for='_h28myd6ub_2']");
        }

        // ── 파일 첨부 (네트워크 상태 이미지) ──
        public async Task AttachFile(string[] filePaths)
        {
            var valid = System.Array.FindAll(filePaths, p => !string.IsNullOrEmpty(p) && File.Exists(p));
            if (valid.Length == 0) return;
            try
            {
                await _page.SetInputFilesAsync("#dropZone input[type='file']", valid,
                    new PageSetInputFilesOptions { Timeout = 10000 });
                await _page.WaitForTimeoutAsync(1000);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("파일첨부 오류: " + ex.Message);
            }
        }

        // ── 원격 담당자 (조직도 검색) ──
        public async Task FillRemoteManager(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        // 팝업이 열려있으면 닫기
                        await SafeClick("a.btn_layer_x");
                        await _page.WaitForTimeoutAsync(400);
                    }

                    // 재시도 시: 이미 이름이 들어가 있으면 종료
                    if (attempt > 0)
                    {
                        bool alreadySet = await _page.EvaluateAsync<bool>(
                            "n => (document.querySelector('[data-cid=\"_98c44zcu9\"]')?.innerText ?? '').includes(n)", name);
                        if (alreadySet) return;
                    }

                    await _page.ClickAsync("[data-cid='_98c44zcu9'] .add-btn", new PageClickOptions { Timeout = 4000 });
                    await _page.WaitForSelectorAsync("input.input_txt[type='search']", new PageWaitForSelectorOptions { Timeout = 8000 });
                    await _page.WaitForTimeoutAsync(300); // 팝업 UI 안정화

                    var searchInput = _page.Locator("input.input_txt[type='search']");
                    await searchInput.ClickAsync(); // 포커스 명시적 확보
                    await _page.WaitForTimeoutAsync(200);
                    await searchInput.ClearAsync();
                    await searchInput.TypeAsync(name, new LocatorTypeOptions { Delay = 80 }); // 느린 타이핑으로 이벤트 확실히 트리거
                    await _page.WaitForTimeoutAsync(700); // 디바운스 + 검색 결과 로드 대기

                    await _page.WaitForFunctionAsync(
                        "name => Array.from(document.querySelectorAll('div.member')).some(el => el.innerText.includes(name))",
                        name,
                        new PageWaitForFunctionOptions { Timeout = 10000 });

                    await _page.WaitForTimeoutAsync(300); // 목록 렌더링 안정화

                    var member = _page.Locator("div.member").Filter(new LocatorFilterOptions { HasText = name });
                    await member.First.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 5000 });
                    await _page.WaitForTimeoutAsync(1500); // 선택 서버 반영 대기 (핵심)

                    await SafeClick("a.btn_layer_x");
                    await _page.WaitForTimeoutAsync(1000); // DOM 업데이트 대기

                    bool selected = await _page.EvaluateAsync<bool>(
                        "n => (document.querySelector('[data-cid=\"_98c44zcu9\"]')?.innerText ?? '').includes(n)", name);

                    if (selected) return;
                    System.Diagnostics.Debug.WriteLine("원격담당자 재시도 " + (attempt + 1));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("원격담당자 오류 (attempt " + attempt + "): " + ex.Message);
                    await _page.WaitForTimeoutAsync(1000);
                }
            }
        }

        private async Task SafeClick(string selector)
        {
            try { await _page.ClickAsync(selector, new PageClickOptions { Timeout = 3000 }); }
            catch { }
        }
    }
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void SendCtrlV()
        {
            keybd_event(VK_CONTROL, 0, 0, 0);
            keybd_event(VK_V, 0, 0, 0);
            System.Threading.Thread.Sleep(50);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        }
    }

}