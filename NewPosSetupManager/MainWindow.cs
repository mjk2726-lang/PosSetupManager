using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using NewPosSetupManager.Services;

namespace NewPosSetupManager
{
    public class MainWindow : Form
    {
        private WebView2 _webView;
        private WorkspaceManager _workspace;
        private BridgeHandler _bridge;

        public MainWindow()
        {
            Text = "POS Setup Manager";
            Size = new System.Drawing.Size(1440, 900);
            MinimumSize = new System.Drawing.Size(1100, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = System.Drawing.Color.FromArgb(245, 247, 250);

            _workspace = new WorkspaceManager();

            _webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_webView);

            Load += OnLoad;
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, null, null);
                await _webView.EnsureCoreWebView2Async(env);

                string wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "app.local", wwwrootPath,
                    CoreWebView2HostResourceAccessKind.Allow);

                _bridge = new BridgeHandler(_webView, _workspace, this);
                _webView.WebMessageReceived += _bridge.OnWebMessageReceived;

                _webView.CoreWebView2.Navigate("https://app.local/index.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 초기화 실패: " + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _workspace.Save();
            base.OnFormClosed(e);
        }
    }
}
