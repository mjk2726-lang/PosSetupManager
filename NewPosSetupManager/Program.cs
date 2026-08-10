using System;
using System.Windows.Forms;

namespace NewPosSetupManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Forms.SettingsDialog.LoadSettingsFile();
            Application.Run(new MainWindow());
        }
    }
}
