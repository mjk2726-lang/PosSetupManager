using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace PosSetupManager.Forms
{
    public class SettingsDialog : Form
    {
        private TextBox txtId, txtPw, txtSavePath;
        private CheckBox chkAutoReport;
        private FluentButton btnSave, btnCancel, btnBrowse;

        private static readonly string SettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSetupManager", "settings.dat");

        public SettingsDialog()
        {
            this.Text = "설정";
            this.Size = new Size(420, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = FluentColors.Background;
            this.Font = FluentFonts.Body;

            int y = 20;

            // ── 다우오피스 계정 ──
            var lblSection1 = new Label { Text = "다우오피스 로그인 정보", Font = FluentFonts.BodyBold, ForeColor = FluentColors.TextSecond, Location = new Point(24, y), AutoSize = true };
            this.Controls.Add(lblSection1); y += 24;

            var lblId = new Label { Text = "아이디", Location = new Point(24, y), AutoSize = true, ForeColor = FluentColors.TextSecond, Font = FluentFonts.Caption };
            this.Controls.Add(lblId); y += 18;
            txtId = new TextBox { Location = new Point(24, y), Width = 356, BorderStyle = BorderStyle.FixedSingle, Font = FluentFonts.Body };
            this.Controls.Add(txtId); y += 32;

            var lblPw = new Label { Text = "비밀번호", Location = new Point(24, y), AutoSize = true, ForeColor = FluentColors.TextSecond, Font = FluentFonts.Caption };
            this.Controls.Add(lblPw); y += 18;
            txtPw = new TextBox { Location = new Point(24, y), Width = 356, BorderStyle = BorderStyle.FixedSingle, Font = FluentFonts.Body, UseSystemPasswordChar = true };
            this.Controls.Add(txtPw); y += 36;

            // ── 저장 폴더 ──
            var lblSection2 = new Label { Text = "보고서 저장 위치", Font = FluentFonts.BodyBold, ForeColor = FluentColors.TextSecond, Location = new Point(24, y), AutoSize = true };
            this.Controls.Add(lblSection2); y += 24;

            var lblPath = new Label { Text = "저장 폴더", Location = new Point(24, y), AutoSize = true, ForeColor = FluentColors.TextSecond, Font = FluentFonts.Caption };
            this.Controls.Add(lblPath); y += 18;
            txtSavePath = new TextBox { Location = new Point(24, y), Width = 280, BorderStyle = BorderStyle.FixedSingle, Font = FluentFonts.Body };
            btnBrowse = new FluentButton { Text = "찾아보기", Location = new Point(312, y - 1), Width = 68, Height = 26, IsPrimary = false };
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.AddRange(new Control[] { txtSavePath, btnBrowse }); y += 44;

            // ── 메모장 자동 생성 ──
            var lblSection3 = new Label { Text = "기타 설정", Font = FluentFonts.BodyBold, ForeColor = FluentColors.TextSecond, Location = new Point(24, y), AutoSize = true };
            this.Controls.Add(lblSection3); y += 24;

            chkAutoReport = new CheckBox
            {
                Text = "다우오피스 등록 완료 시 메모장 자동 생성",
                Location = new Point(24, y),
                AutoSize = true,
                Font = FluentFonts.Body,
                ForeColor = FluentColors.TextPrimary,
                Checked = true
            };
            this.Controls.Add(chkAutoReport); y += 36;

            // ── 버튼 ──
            btnSave = new FluentButton { Text = "저장", IsPrimary = true, Location = new Point(24, y), Width = 170 };
            btnSave.Click += BtnSave_Click;
            btnCancel = new FluentButton { Text = "취소", IsPrimary = false, Location = new Point(204, y), Width = 176 };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });

            LoadSettings();
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog { Description = "보고서 저장 폴더를 선택해주세요." };
            if (!string.IsNullOrEmpty(txtSavePath.Text) && Directory.Exists(txtSavePath.Text))
                dlg.SelectedPath = txtSavePath.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
                txtSavePath.Text = dlg.SelectedPath;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtId.Text) && !string.IsNullOrWhiteSpace(txtPw.Text))
                CredentialStore.Save(txtId.Text.Trim(), txtPw.Text);
            SavePath = txtSavePath.Text;
            AutoReportEnabled = chkAutoReport.Checked;
            SaveSettings();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void LoadSettings()
        {
            if (CredentialStore.Load(out string id, out string pw))
            {
                txtId.Text = id;
                txtPw.Text = pw;
            }
            LoadSettingsFile();
            txtSavePath.Text = SavePath ?? "";
            chkAutoReport.Checked = AutoReportEnabled;
        }

        // ── 설정 관리 ──
        public static string SavePath { get; private set; }
        public static bool AutoReportEnabled { get; private set; } = true;

        public static string GetSavePath()
        {
            return string.IsNullOrEmpty(SavePath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : SavePath;
        }

        public static void LoadSettingsFile()
        {
            try
            {
                if (!File.Exists(SettingsFile)) return;
                var content = File.ReadAllText(SettingsFile).Trim();
                var parts = content.Split('|');
                SavePath = parts[0];
                AutoReportEnabled = parts.Length < 2 || parts[1] != "false";
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
                File.WriteAllText(SettingsFile,
                    (SavePath ?? "") + "|" + (AutoReportEnabled ? "true" : "false"));
            }
            catch { }
        }
    }
}