using System;
using System.Windows.Forms;
using PosSetupManager.Forms;

namespace PosSetupManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // 앱 시작 시 설정 로드
            SettingsDialog.LoadSettingsFile();
            Application.Run(new MainForm());
        }
    }
}