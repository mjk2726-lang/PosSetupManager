using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        public List<ScheduleItem> ExtractedItems { get; private set; }

        private static string DataDir => Path.Combine(
            Path.GetDirectoryName(Application.ExecutablePath), "CalendarProfile");

        public CalendarForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "다우오피스 캘린더";
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
                    _webView.CoreWebView2.Navigate(CalendarReader.BaseUrl);
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

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnNavigationCompleted(sender, e))); return; }
            _urlBar.Text = _webView.Source?.ToString() ?? "";
            _extractBtn.Enabled = true;
            _statusLabel.Text = "페이지 로드 완료 - '오늘 일정 가져오기' 버튼을 클릭하세요";
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

                if (items.Count == 0)
                {
                    _statusLabel.Text = "추출된 일정 없음. '진단' 버튼으로 페이지 구조를 확인해보세요.";
                    _extractBtn.Enabled = true;
                    return;
                }

                var preview = new System.Text.StringBuilder();
                foreach (var it in items)
                    preview.AppendLine(string.Format("• {0}{1}",
                        it.StoreName,
                        string.IsNullOrEmpty(it.InstallTime) ? "" : "  " + it.InstallTime));

                var msg = string.Format("{0}개 일정을 추출했습니다:\n\n{1}\n세션으로 추가하시겠습니까?",
                    items.Count, preview);
                if (MessageBox.Show(msg, "일정 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    ExtractedItems = items;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _statusLabel.Text = string.Format("{0}개 추출됨 (적용 취소됨)", items.Count);
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

