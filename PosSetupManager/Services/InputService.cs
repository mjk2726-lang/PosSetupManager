using System;
using System.Collections.Generic;
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
                // 시간 형식 보정 (예: "12" → "12:00", "9" → "09:00")
                string formattedTime = "";
                if (!string.IsNullOrEmpty(time))
                {
                    if (time.Contains(":"))
                    {
                        // "12:0" → "12:00"
                        var parts = time.Split(':');
                        formattedTime = string.Format("{0:D2}:{1:D2}",
                            int.Parse(parts[0]),
                            parts.Length > 1 ? int.Parse(parts[1]) : 0);
                    }
                    else
                    {
                        // "12" → "12:00"
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

        // ── 라디오/O·X — label 텍스트로 찾아 클릭 ──
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
            string[] posOrder = {
                "이지포스","엠포스","스마일포스","오케이포스","메이트포스","배달포스",
                "K포스","하이픈포스(개통불가)","유니온포스","키움포스","팝스(웨이브포스)","링크포스",
                "포스마스터(티페이)","퍼스트포스","에어포스","토스포스","포스메이커스(개통불가)",
                "윙스포스","안시포스","기타","연동안함","타밴","우리밴"
            };

            for (int i = 0; i < posOrder.Length; i++)
            {
                if (posTypes.Contains(posOrder[i]))
                {
                    var sel = string.Format("[data-cid='_usvoeowq9'] label[for='_usvoeowq9_{0}']", i);
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

        // ── 원격 담당자 (조직도 검색) ──
        public async Task FillRemoteManager(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                // 추가 버튼 클릭
                await _page.ClickAsync("[data-cid='_98c44zcu9'] .add-btn", new PageClickOptions { Timeout = 3000 });
                await _page.WaitForTimeoutAsync(800);

                // 검색창에 이름 입력 (nativeInputValueSetter 방식)
                var searchJs = string.Format(@"
                    var input = document.querySelector('input.input_txt[type=""search""]');
                    if(input) {{
                        var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                        setter.call(input, '{0}');
                        input.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    }}
                ", name);
                await _page.EvaluateAsync(searchJs);
                await _page.WaitForTimeoutAsync(1500);

                // Playwright Locator로 div.member 클릭
                try
                {
                    var members = _page.Locator("div.member");
                    int count = await members.CountAsync();
                    for (int i = 0; i < count; i++)
                    {
                        var text = await members.Nth(i).InnerTextAsync();
                        if (text.Contains(name))
                        {
                            await members.Nth(i).ClickAsync(new LocatorClickOptions { Force = true, Timeout = 3000 });
                            break;
                        }
                    }
                }
                catch { }
                await _page.WaitForTimeoutAsync(500);

                // 닫기 버튼 클릭
                await SafeClick("a.btn_layer_x");
                await _page.WaitForTimeoutAsync(300);
            }
            catch { }
        }

        private async Task SafeClick(string selector)
        {
            try { await _page.ClickAsync(selector, new PageClickOptions { Timeout = 3000 }); }
            catch { }
        }
    }
}