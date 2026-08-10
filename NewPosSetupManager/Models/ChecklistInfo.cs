using System;

namespace NewPosSetupManager.Models
{
    [Serializable]
    public class ChecklistInfo
    {
        public string CheckExternalIP { get; set; } = "";
        public string CheckDHCP { get; set; } = "";
        // 로컬모드 체크박스 (메뉴판/알림판)
        public bool LocalModeMenuBoard { get; set; } = false;
        public bool LocalModeNoticeBoard { get; set; } = false;
        public string CheckFirewall { get; set; } = "";
        public string CheckFirewallPopup { get; set; } = "";
        public string OrderPosCount { get; set; } = "X";
        public string OrderPosNote { get; set; } = "";
        public string CheckHiorderLogin { get; set; } = "";
        public string CheckSyncOrder { get; set; } = "";
        // 선불 매장
        public bool PrepaidTableCheck { get; set; } = false;
        public bool PrepaidPaymentCheck { get; set; } = false;
        public bool PrepaidOver5Man { get; set; } = false;
        public bool PrepaidKSNET { get; set; } = false;
        public string CheckTableSort { get; set; } = "";
        // "좋음" | "양호" | "불량"
        public string WifiStatus { get; set; } = "";
        public string CheckMenuImage { get; set; } = "";
        public string CheckNoticeBoardVer { get; set; } = "";
        public string CheckNoticeBoardAdmin { get; set; } = "";
        public string CheckMenuBoardVer { get; set; } = "";
        public string CheckMenuBoardAutoRun { get; set; } = "";
        public string CheckCoupon { get; set; } = "";
    }
}
