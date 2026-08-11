using System;
using System.Collections.Generic;

namespace NewPosSetupManager.Models
{
    [Serializable]
    public class BasicInfo
    {
        public string StoreName { get; set; } = "";
        public string StoreId { get; set; } = "";
        public string InstallDate { get; set; } = "";
        public string InstallTime { get; set; } = "";
        public string RemoteManager { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string LinkEndTime { get; set; } = "";
        public string ElapsedTime { get; set; } = "";
        public string EngineerContact { get; set; } = "";
        public string TableMode { get; set; } = "";
        public string AttachmentPath { get; set; } = ""; // 하위호환용
        public List<string> AttachmentPaths { get; set; } = new List<string>();
    }
}
