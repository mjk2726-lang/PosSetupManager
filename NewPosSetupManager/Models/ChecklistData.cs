using System;

namespace NewPosSetupManager.Models
{
    [Serializable]
    public class ChecklistData
    {
        public BasicInfo Basic { get; set; } = new BasicInfo();
        public PosInfo Pos { get; set; } = new PosInfo();
        public NetworkInfo Network { get; set; } = new NetworkInfo();
        public ChecklistInfo Checklist { get; set; } = new ChecklistInfo();
        public FinishInfo Finish { get; set; } = new FinishInfo();

        // 작업 상태 (멀티 매장 대비)
        // "작성중" | "완료"
        public string Status { get; set; } = "작성중";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}
