using System;

namespace NewPosSetupManager.Models
{
    public class ScheduleItem
    {
        public string RawTitle { get; set; } = "";
        public string StoreName { get; set; } = "";
        public string TableMode { get; set; } = "";
        public DateTime? InstallDate { get; set; }
        public string InstallTime { get; set; } = "";
        public string RemoteManager { get; set; } = "";
        public string EngineerContact { get; set; } = "";
    }
}

