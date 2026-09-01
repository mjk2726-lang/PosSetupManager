using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using NewPosSetupManager.Models;
using NewPosSetupManager.Services;

namespace NewPosSetupManager.Forms
{
    public class CalendarForm : Form
    {
        private WebView2 _webView;
        private TextBox _urlBar;
        private Label _statusLabel;
        private Button _extractBtn;
        private readonly string _lookupName;
        private readonly HashSet<string> _existingStoreNames;
        private bool _loginAttempted;

        public List<ScheduleItem> ExtractedItems { get; private set; }

        private static string DataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NewPosSetupManager", "CalendarProfile");

        public CalendarForm(string lookupName, IEnumerable<string> existingStoreNames)
        {
            _lookupName = (lookupName ?? "").Trim();
            _existingStoreNames = new HashSet<string>(
                existingStoreNames ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "다우오피스 캘린더 - " + _lookupName;
            Size = new Size(1100, 740);
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("맑은 고딕", 10f);

            // ── 상단 주소바 ──
            var topPanel = new Panel
            {
                Dock = DockStyle.Top, Height = 42,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(6, 6, 6, 0)
            };
            var devBtn = MakeTopBtn("F12", Color.FromArgb(100, 100, 100), 44);
            devBtn.Click += (s, e) => _webView?.CoreWebView2?.OpenDevToolsWindow();
            var goBtn = MakeTopBtn("이동", Color.FromArgb(70, 130, 180), 60);
            goBtn.Click += (s, e) => Navigate(_urlBar.Text.Trim());
            _urlBar = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _urlBar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { Navigate(_urlBar.Text.Trim()); e.SuppressKeyPress = true; }
            };
            topPanel.Controls.Add(_urlBar);
            topPanel.Controls.Add(goBtn);
            topPanel.Controls.Add(devBtn);

            // ── 하단 버튼바 ──
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(8, 8, 8, 8)
            };
            _extractBtn = new Button
            {
                Text = "오늘 일정 가져오기",
                Dock = DockStyle.Right, Width = 160,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
            };
            _extractBtn.FlatAppearance.BorderSize = 0;
            _extractBtn.Click += ExtractBtn_Click;

            var titleTestBtn = MakeBottomBtn("제목테스트", Color.FromArgb(60, 120, 120));
            titleTestBtn.Click += TitleTestBtn_Click;

            var diagBtn = MakeBottomBtn("진단", Color.FromArgb(140, 100, 50));
            diagBtn.Click += DiagBtn_Click;

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            bottomPanel.Controls.Add(_extractBtn);
            bottomPanel.Controls.Add(diagBtn);
            bottomPanel.Controls.Add(titleTestBtn);
            bottomPanel.Controls.Add(_statusLabel);

            // ── WebView2 ──
            _webView = new WebView2 { Dock = DockStyle.Fill };

            Controls.Add(_webView);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            Load += async (s, e) =>
            {
                try
                {
                    Directory.CreateDirectory(DataDir);
                    var env = await CoreWebView2Environment.CreateAsync(null, DataDir);
                    await _webView.EnsureCoreWebView2Async(env);
                    _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                    _webView.CoreWebView2.Navigate(CalendarReader.TodayAgendaUrl());
                    _statusLabel.Text = "캘린더 페이지로 이동 중...";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("WebView2 초기화 실패:\n" + ex.Message +
                        "\n\nMicrosoft Edge WebView2 런타임이 설치되어 있는지 확인해주세요.",
                        "초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private async void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => OnNavigationCompleted(sender, e))); return; }
            _urlBar.Text = _webView.Source?.ToString() ?? "";

            if (await HasLoginFormAsync())
            {
                _extractBtn.Enabled = false;
                if (_loginAttempted)
                {
                    _statusLabel.Text = "자동 로그인에 실패했습니다. 설정의 아이디/비밀번호를 확인해주세요.";
                    return;
                }

                if (!CredentialStore.Load(out string id, out string pw))
                {
                    _statusLabel.Text = "저장된 로그인 정보가 없습니다. 설정에서 먼저 저장해주세요.";
                    return;
                }

                _loginAttempted = true;
                _statusLabel.Text = "저장된 계정으로 자동 로그인 중...";
                try
                {
                    var idJson = JsonConvert.SerializeObject(id);
                    var pwJson = JsonConvert.SerializeObject(pw);
                    var script = @"(function(id, pw) {
  var idInput = document.querySelector(""input.input_txt[type='text'], input[name='id'], input[name='username'], input[placeholder*='아이디'], input[type='text']"");
  var pwInput = document.querySelector(""input[name='password'], input[placeholder*='비밀번호'], input[type='password']"");
  var submit = document.querySelector(""button[type='submit'], input[type='submit'], .btn_login, .login_btn"");
  if (!submit) {
    submit = Array.from(document.querySelectorAll('button, a')).find(function(el) {
      return (el.innerText || el.textContent || '').trim() === '로그인';
    });
  }
  if (!idInput || !pwInput || !submit) return false;
  var setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
  setter.call(idInput, id);
  idInput.dispatchEvent(new Event('input', { bubbles: true }));
  idInput.dispatchEvent(new Event('change', { bubbles: true }));
  setter.call(pwInput, pw);
  pwInput.dispatchEvent(new Event('input', { bubbles: true }));
  pwInput.dispatchEvent(new Event('change', { bubbles: true }));
  submit.click();
  return true;
})(" + idJson + ", " + pwJson + ")";
                    var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                    if (!string.Equals(result, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        _statusLabel.Text = "로그인 입력칸을 찾지 못했습니다. 페이지를 확인해주세요.";
                        return;
                    }

                    _statusLabel.Text = "로그인 처리 중...";
                    for (int attempt = 0; attempt < 60; attempt++)
                    {
                        await System.Threading.Tasks.Task.Delay(250);
                        if (!await HasLoginInputsNowAsync())
                        {
                            _statusLabel.Text = "로그인 완료 - 오늘 일정으로 이동 중...";
                            _webView.CoreWebView2.Navigate(CalendarReader.TodayAgendaUrl());
                            return;
                        }
                    }
                    _statusLabel.Text = "자동 로그인 시간이 초과되었습니다. 아이디/비밀번호를 확인해주세요.";
                }
                catch (Exception ex)
                {
                    _statusLabel.Text = "자동 로그인 오류: " + ex.Message;
                }
                return;
            }

            _loginAttempted = false;
            _extractBtn.Enabled = true;
            _statusLabel.Text = "페이지 로드 완료 - '오늘 일정 가져오기' 버튼을 클릭하세요";
        }

        private static bool IsLoginPage(string url)
        {
            return !string.IsNullOrEmpty(url)
                && (url.IndexOf("login", StringComparison.OrdinalIgnoreCase) >= 0
                    || url.IndexOf("signin", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private async System.Threading.Tasks.Task<bool> HasLoginFormAsync()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    if (await HasLoginInputsNowAsync())
                        return true;
                }
                catch
                {
                    if (IsLoginPage(_urlBar.Text)) return true;
                }

                if (!IsLoginPage(_urlBar.Text)) return false;
                await System.Threading.Tasks.Task.Delay(250);
            }

            return IsLoginPage(_urlBar.Text);
        }

        private async System.Threading.Tasks.Task<bool> HasLoginInputsNowAsync()
        {
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(@"
(function() {
  var idInput = document.querySelector(""input.input_txt[type='text'], input[name='id'], input[name='username'], input[placeholder*='아이디'], input[type='text']"");
  var pwInput = document.querySelector(""input[name='password'], input[placeholder*='비밀번호'], input[type='password']"");
  return !!(idInput && pwInput);
})()");
            return string.Equals(result, "true", StringComparison.OrdinalIgnoreCase);
        }

        private void Navigate(string url)
        {
            if (_webView?.CoreWebView2 == null) return;
            if (!url.StartsWith("http")) url = "https://" + url;
            _webView.CoreWebView2.Navigate(url);
            _statusLabel.Text = "이동 중...";
            _extractBtn.Enabled = false;
        }

        private async void ExtractBtn_Click(object sender, EventArgs e)
        {
            if (_webView?.CoreWebView2 == null) return;
            try
            {
                _statusLabel.Text = "일정 추출 중...";
                _extractBtn.Enabled = false;

                var rawJson = await _webView.CoreWebView2.ExecuteScriptAsync(CalendarReader.GetExtractScript());
                // ExecuteScriptAsync는 JSON 인코딩된 문자열 반환 → 디코딩
                var innerJson = JsonConvert.DeserializeObject<string>(rawJson);
                var items = CalendarReader.ParseExtracted(innerJson, DateTime.Today);
                var namePattern = @"\(\s*" + Regex.Escape(_lookupName) + @"\s*\)";
                items = items
                    .Where(i => Regex.IsMatch(i.RawTitle ?? "", namePattern, RegexOptions.IgnoreCase))
                    .GroupBy(i => i.StoreName, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                if (items.Count == 0)
                {
                    _statusLabel.Text = "오늘 일정 중 (" + _lookupName + ") 일정이 없습니다.";
                    _extractBtn.Enabled = true;
                    return;
                }

                var selected = ShowSelectionDialog(items);
                if (selected != null && selected.Count > 0)
                {
                    ExtractedItems = selected;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _statusLabel.Text = string.Format("{0}개 조회됨 (가져오기 취소)", items.Count);
                    _extractBtn.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "오류: " + ex.Message;
                _extractBtn.Enabled = true;
                MessageBox.Show("추출 중 오류:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<ScheduleItem> ShowSelectionDialog(List<ScheduleItem> items)
        {
            using var dialog = new Form
            {
                Text = "오늘의 담당 일정 선택",
                Size = new Size(520, 460),
                MinimumSize = new Size(440, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = new Font("맑은 고딕", 10f)
            };

            var guide = new Label
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(12, 10, 12, 6),
                Text = string.Format("오늘 일정 중 ({0}) 항목입니다.\r\n가져올 매장을 선택하세요. 중복 매장은 기본 제외됩니다.", _lookupName),
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            var list = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            foreach (var item in items)
            {
                bool duplicate = _existingStoreNames.Contains(item.StoreName);
                int index = list.Items.Add(new CalendarSelectionItem(item, duplicate));
                list.SetItemChecked(index, !duplicate);
            }

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(10) };
            var btnImport = new Button
            {
                Text = "선택 항목 가져오기",
                Dock = DockStyle.Right,
                Width = 150,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnImport.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button { Text = "취소", Dock = DockStyle.Right, Width = 90 };
            btnImport.Click += (s, e) =>
            {
                if (list.CheckedIndices.Count == 0)
                {
                    MessageBox.Show(dialog, "가져올 매장을 하나 이상 선택해주세요.", "선택 필요",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                dialog.DialogResult = DialogResult.OK;
            };
            btnCancel.Click += (s, e) => dialog.DialogResult = DialogResult.Cancel;
            bottom.Controls.Add(btnImport);
            bottom.Controls.Add(btnCancel);
            dialog.Controls.Add(list);
            dialog.Controls.Add(guide);
            dialog.Controls.Add(bottom);
            dialog.AcceptButton = btnImport;
            dialog.CancelButton = btnCancel;

            if (dialog.ShowDialog(this) != DialogResult.OK) return null;
            var selected = new List<ScheduleItem>();
            foreach (int index in list.CheckedIndices)
                selected.Add(((CalendarSelectionItem)list.Items[index]).Item);
            return selected;
        }

        private sealed class CalendarSelectionItem
        {
            public ScheduleItem Item { get; }
            private readonly bool _duplicate;

            public CalendarSelectionItem(ScheduleItem item, bool duplicate)
            {
                Item = item;
                _duplicate = duplicate;
            }

            public override string ToString()
            {
                return Item.StoreName + (_duplicate ? "  [이미 작업목록에 있음]" : "");
            }
        }

        private async void TitleTestBtn_Click(object sender, EventArgs e)
        {
            if (_webView?.CoreWebView2 == null) return;
            var title = await _webView.CoreWebView2.ExecuteScriptAsync("document.title");
            MessageBox.Show("현재 페이지 제목: " + title, "제목 테스트");
        }

        private async void DiagBtn_Click(object sender, EventArgs e)
        {
            if (_webView?.CoreWebView2 == null) return;
            _statusLabel.Text = "진단 중...";
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(CalendarReader.GetDiagScript());
            var decoded = JsonConvert.DeserializeObject<string>(result) ?? result;
            MessageBox.Show("캘린더 관련 클래스 분포:\n\n" + decoded, "진단 결과");
            _statusLabel.Text = "진단 완료";
        }

        private Button MakeTopBtn(string text, Color bg, int w)
        {
            var btn = new Button
            {
                Text = text, Width = w, Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9f), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button MakeBottomBtn(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text, Width = 90, Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9f), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}

