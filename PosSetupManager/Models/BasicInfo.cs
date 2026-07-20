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
        public string EngineerContact { get; set; } = ""; // 현장 엔지니어 연락처 (프로그램 내부용)
        public string TableMode { get; set; } = ""; // 기본정보에서 선택
    }
}