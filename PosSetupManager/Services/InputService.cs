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

            foreach (var posType in posTypes)
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
        public async Task AttachFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            try
            {
                // 1. 이미지를 클립보드에 복사 (STA 스레드)
                var thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        using (var img = System.Drawing.Image.FromFile(filePath))
                        using (var bmp = new System.Drawing.Bitmap(img))
                            System.Windows.Forms.Clipboard.SetImage(bmp);
                    }
                    catch { }
                });
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
                thread.Join();
                await _page.WaitForTimeoutAsync(300);

                // 2. iframe id로 찾기 (dext_frame_editor)
                Microsoft.Playwright.IFrame frame = null;
                foreach (var f in _page.Frames)
                {
                    // name이 비어있고 id=dext_frame_editor
                    if (f.Url.Contains("editor_release")) { frame = f; break; }
                }

                // Playwright frame으로 직접 접근 (name=dext_frame_editor)
                var editorFrame = _page.Frame("dext_frame_editor");
                if (editorFrame != null)
                {
                    // frame 안 body 클릭
                    await editorFrame.ClickAsync("body", new FrameClickOptions { Timeout = 5000 });
                    await _page.WaitForTimeoutAsync(500);

                    // Playwright keyboard Ctrl+V (frame 포커스 후)
                    await editorFrame.Locator("body").PressAsync("Control+v");
                    await _page.WaitForTimeoutAsync(2000);

                    // 이미지 보정 팝업 - dext_dialog_editor frame 안에 있음
                    try
                    {
                        await _page.WaitForTimeoutAsync(2000);
                        var dialogFrame = _page.Frame("dext_dialog_editor");
                        if (dialogFrame != null)
                        {
                            await dialogFrame.ClickAsync("#image_btn",
                                new FrameClickOptions { Force = true, Timeout = 5000 });
                        }
                        await _page.WaitForTimeoutAsync(1000);
                    }
                    catch { }
                }
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
            try
            {
                await _page.ClickAsync("[data-cid='_98c44zcu9'] .add-btn", new PageClickOptions { Timeout = 3000 });

                // 팝업의 검색 input이 실제로 나타날 때까지 대기
                await _page.WaitForSelectorAsync(
                    "input.input_txt[type='search']",
                    new PageWaitForSelectorOptions { Timeout = 8000 });

                var searchJs = string.Format(@"
                    var input = document.querySelector('input.input_txt[type=""search""]');
                    if(input) {{
                        var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                        setter.call(input, '{0}');
                        input.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    }}
                ", name);
                await _page.EvaluateAsync(searchJs);

                // 검색 결과가 실제로 나타날 때까지 대기
                await _page.WaitForSelectorAsync(
                    "div.member",
                    new PageWaitForSelectorOptions { Timeout = 8000 });

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
                await _page.WaitForTimeoutAsync(300);
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