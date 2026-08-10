using System;

namespace NewPosSetupManager.Models
{
    [Serializable]
    public class FinishInfo
    {
        public string CouponXReason { get; set; } = "";
        public string RemoteEduContact { get; set; } = "";
        public string InstallIssue { get; set; } = "";
        public string SmsMessage { get; set; } = "";
    }
}
