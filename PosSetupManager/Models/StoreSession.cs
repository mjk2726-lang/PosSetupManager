using System;
using PosSetupManager.Models;

namespace PosSetupManager.Models
{
    [Serializable]
    public class StoreSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ChecklistData Data { get; set; } = new ChecklistData();

        // "작성중" | "완료"
        public string Status { get; set; } = "작성중";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        // 표시용
        public string DisplayName
        {
            get
            {
                var name = Data?.Basic?.StoreName;
                return string.IsNullOrEmpty(name) ? "(매장명 미입력)" : name;
            }
        }

        public string StatusIcon => Status == "완료" ? "🟢" : "🟡";
        public int SortOrder { get; set; } = 0; // 순번
    }
}