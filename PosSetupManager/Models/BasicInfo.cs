using System;

namespace PosSetupManager.Models
{
    [Serializable]
    public class BasicInfo
    {
        public string StoreName { get; set; } = "";
        public DateTime? InstallDate { get; set; }
        public string InstallTime { get; set; } = "";
        public string RemoteManager { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string LinkEndTime { get; set; } = "";
        public string ElapsedTime { get; set; } = "";
    }
}