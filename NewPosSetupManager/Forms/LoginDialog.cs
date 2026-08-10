using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace NewPosSetupManager.Forms
{
    // ── 자격증명 저장/로드 ──
    public static class CredentialStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSetupManager", "credentials.dat");

        public static void Save(string id, string pw)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var data = id + "\n" + pw;
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(data),
                null,
                DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePath, encrypted);
        }

        public static bool Load(out string id, out string pw)
        {
            id = ""; pw = "";
            if (!File.Exists(FilePath)) return false;
            try
            {
                var encrypted = File.ReadAllBytes(FilePath);
                var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var parts = Encoding.UTF8.GetString(decrypted).Split('\n');
                if (parts.Length < 2) return false;
                id = parts[0];
                pw = parts[1];
                return true;
            }
            catch { return false; }
        }

        public static bool Exists() => File.Exists(FilePath);
    }

    // ── 로그인 설정 다이얼로그 ──
    public class LoginDialog : Form
    {
        private TextBox txtId, txtPw;
        private FluentButton btnSave, btnCancel;

        public string DaumId { get; private set; }
        public string DaumPw { get; private set; }

        public LoginDialog()
        {
            this.Text = "다우오피스 로그인 설정";
            this.Size = new Size(380, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = FluentColors.Background;
            this.Font = FluentFonts.Body;

            int y = 20;

            var lblId = new Label { Text = "아이디", Location = new Point(24, y), AutoSize = true, ForeColor = FluentColors.TextSecond };
            this.Controls.Add(lblId); y += 20;

            txtId = new TextBox { Location = new Point(24, y), Width = 316, BorderStyle = BorderStyle.FixedSingle, Font = FluentFonts.Body };
            this.Controls.Add(txtId); y += 36;

            var lblPw = new Label { Text = "비밀번호", Location = new Point(24, y), AutoSize = true, ForeColor = FluentColors.TextSecond };
            this.Controls.Add(lblPw); y += 20;

            txtPw = new TextBox { Location = new Point(24, y), Width = 316, BorderStyle = BorderStyle.FixedSingle, Font = FluentFonts.Body, UseSystemPasswordChar = true };
            this.Controls.Add(txtPw); y += 44;

            btnSave = new FluentButton { Text = "저장", IsPrimary = true, Location = new Point(24, y), Width = 150 };
            btnSave.Click += BtnSave_Click;

            btnCancel = new FluentButton { Text = "취소", IsPrimary = false, Location = new Point(184, y), Width = 156 };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });

            // 저장된 값 불러오기
            if (CredentialStore.Load(out string id, out string pw))
            {
                txtId.Text = id;
                txtPw.Text = pw;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtId.Text) || string.IsNullOrWhiteSpace(txtPw.Text))
            {
                MessageBox.Show("아이디와 비밀번호를 입력해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DaumId = txtId.Text.Trim();
            DaumPw = txtPw.Text;
            CredentialStore.Save(DaumId, DaumPw);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
