using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NewPosSetupManager.Forms;
using NewPosSetupManager.Models;
using NewPosSetupManager.Services;

namespace NewPosSetupManager
{
    public class BridgeHandler
    {
        private readonly WebView2 _webView;
        private readonly WorkspaceManager _workspace;
        private readonly Form _owner;

        public BridgeHandler(WebView2 webView, WorkspaceManager workspace, Form owner)
        {
            _webView = webView;
            _workspace = workspace;
            _owner = owner;
        }

        public void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = JObject.Parse(e.TryGetWebMessageAsString());
                var action = msg["action"]?.ToString();
                switch (action)
                {
                    case "getSessions":      HandleGetSessions(); break;
                    case "saveSession":      HandleSaveSession(msg); break;
                    case "addSession":       HandleAddSession(); break;
                    case "deleteSession":    HandleDeleteSession(msg); break;
                    case "completeSession":  HandleCompleteSession(msg); break;
                    case "restoreSession":   HandleRestoreSession(msg); break;
                    case "reorderSessions":  HandleReorderSessions(msg); break;
                    case "startRegistration":HandleStartRegistration(msg); break;
                    case "selectFiles":      HandleSelectFiles(); break;
                    case "callPhone":        HandleCallPhone(msg); break;
                    case "sendSms":          HandleSendSms(msg); break;
                    case "openCalendar":     HandleOpenCalendar(); break;
                    case "openSettings":     HandleOpenSettings(); break;
                    case "deleteBatch":      HandleDeleteBatch(msg); break;
                    case "setTitle":         _owner.Invoke((Action)(() => _owner.Text = msg["title"]?.ToString())); break;
                }
            }
            catch (Exception ex)
            {
                Send(new { type = "error", message = ex.Message });
            }
        }

        private void HandleGetSessions()
        {
            Send(new
            {
                type = "sessions",
                active = _workspace.ActiveSessions,
                completed = _workspace.CompletedSessions
            });
        }

        private void HandleSaveSession(JObject msg)
        {
            var id = msg["sessionId"]?.ToString();
            var data = msg["data"]?.ToObject<ChecklistData>();
            var session = _workspace.ActiveSessions.Find(s => s.Id == id)
                        ?? _workspace.CompletedSessions.Find(s => s.Id == id);
            if (session == null || data == null) return;
            session.Data = data;
            _workspace.Save();
            Send(new { type = "sessionSaved" });
        }

        private void HandleAddSession()
        {
            var session = _workspace.AddSession();
            _workspace.Save();
            Send(new { type = "sessionAdded", session });
        }

        private void HandleDeleteSession(JObject msg)
        {
            var id = msg["sessionId"]?.ToString();
            _workspace.ActiveSessions.RemoveAll(s => s.Id == id);
            _workspace.CompletedSessions.RemoveAll(s => s.Id == id);
            _workspace.Save();
            Send(new { type = "sessionDeleted", id });
        }

        private void HandleCompleteSession(JObject msg)
        {
            var id = msg["sessionId"]?.ToString();
            _workspace.CompleteSession(id);
            Send(new { type = "sessionCompleted", id });
        }

        private void HandleRestoreSession(JObject msg)
        {
            var id = msg["sessionId"]?.ToString();
            var session = _workspace.CompletedSessions.Find(s => s.Id == id);
            if (session == null) return;
            session.Status = "작성중";
            session.CompletedAt = null;
            _workspace.CompletedSessions.Remove(session);
            _workspace.ActiveSessions.Add(session);
            _workspace.Save();
            Send(new { type = "sessionRestored", id });
        }

        private void HandleReorderSessions(JObject msg)
        {
            var ids = msg["ids"]?.ToObject<string[]>();
            if (ids == null) return;
            var reordered = ids
                .Select(id => _workspace.ActiveSessions.Find(s => s.Id == id))
                .Where(s => s != null)
                .ToList();
            _workspace.ActiveSessions.Clear();
            _workspace.ActiveSessions.AddRange(reordered);
            _workspace.Save();
            Send(new { type = "sessionsReordered" });
        }

        private async void HandleStartRegistration(JObject msg)
        {
            var id = msg["sessionId"]?.ToString();
            var session = _workspace.ActiveSessions.Find(s => s.Id == id)
                        ?? _workspace.CompletedSessions.Find(s => s.Id == id);
            if (session == null) return;

            try
            {
                var svc = new PlaywrightService();
                var progress = new Progress<string>(m =>
                    Send(new { type = "registrationProgress", message = m }));
                var result = await svc.RegisterAsync(session.Data, progress);
                var eduSkipped = !System.Text.RegularExpressions.Regex.IsMatch(
                    session.Data.Finish.RemoteEduContact ?? "", @"^010-\d{4}-\d{4}$");

                if (result.Item1)
                    ReportService.SaveReport(session.Data);

                Send(new { type = "registrationResult", success = result.Item1, message = result.Item2, eduSkipped });
            }
            catch (Exception ex)
            {
                Send(new { type = "registrationResult", success = false, message = ex.Message });
            }
        }

        private void HandleSelectFiles()
        {
            _owner.Invoke((Action)(() =>
            {
                using var dlg = new OpenFileDialog
                {
                    Title = "네트워크 상태 이미지 선택",
                    Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif|모든 파일|*.*",
                    Multiselect = true
                };
                if (dlg.ShowDialog(_owner) == DialogResult.OK)
                    Send(new { type = "filesSelected", paths = dlg.FileNames });
            }));
        }

        private void HandleCallPhone(JObject msg)
        {
            var number = (msg["number"]?.ToString() ?? "").Replace("-", "").Replace(" ", "");
            if (string.IsNullOrEmpty(number)) return;
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo("tel:" + number) { UseShellExecute = true });
            }
            catch { }
        }

        private void HandleSendSms(JObject msg)
        {
            var phone = msg["phone"]?.ToString();
            var message = msg["message"]?.ToString() ?? "";
            _owner.Invoke((Action)(() => System.Windows.Forms.Clipboard.SetText(message)));
            try
            {
                var uri = "sms:" + phone + "?body=" + Uri.EscapeDataString(message);
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
                Send(new { type = "smsSent" });
            }
            catch
            {
                Send(new { type = "smsCopied" });
            }
        }

        private void HandleOpenCalendar()
        {
            _owner.Invoke((Action)(() =>
            {
                using var dlg = new CalendarForm();
                if (dlg.ShowDialog(_owner) != DialogResult.OK) return;
                var items = dlg.ExtractedItems;
                if (items == null || items.Count == 0) return;

                foreach (var item in items)
                {
                    var session = _workspace.AddSession();
                    session.Data.Basic.StoreName = item.StoreName;
                    if (item.InstallDate.HasValue)
                        session.Data.Basic.InstallDate = item.InstallDate.Value.ToString("yyyy-MM-dd");
                    session.Data.Basic.InstallTime = item.InstallTime;
                    session.Data.Basic.RemoteManager = item.RemoteManager;
                    session.Data.Basic.EngineerContact = item.EngineerContact;
                }
                _workspace.Save();
                HandleGetSessions();
            }));
        }

        private void HandleOpenSettings()
        {
            _owner.Invoke((Action)(() =>
            {
                using var dlg = new SettingsDialog();
                dlg.ShowDialog(_owner);
            }));
        }

        private void HandleDeleteBatch(JObject msg)
        {
            var ids = msg["ids"]?.ToObject<string[]>();
            if (ids == null) return;
            foreach (var id in ids)
                _workspace.CompletedSessions.RemoveAll(s => s.Id == id);
            _workspace.Save();
            HandleGetSessions();
        }

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include,
            DateFormatString = "yyyy-MM-dd"
        };

        public void Send(object data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.None, _jsonSettings);
            if (_webView.InvokeRequired)
                _webView.Invoke((Action)(() => _webView.CoreWebView2?.PostWebMessageAsString(json)));
            else
                _webView.CoreWebView2?.PostWebMessageAsString(json);
        }
    }
}
