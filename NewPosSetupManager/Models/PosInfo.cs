using System;
using System.Collections.Generic;

namespace NewPosSetupManager.Models
{
    [Serializable]
    public class PosInfo
    {
        // "LMM" | "크롬" | "씨트롬" | "기타"
        public string RemoteAccount { get; set; } = "";
        // "동일" | "다름"
        public string RemoteAdmin { get; set; } = "";
        // "O" | "X"
        public string LmmAccount { get; set; } = "";
        public List<string> PosTypes { get; set; } = new List<string>();
        // "타밴" | "우리밴"
        public string VanType { get; set; } = "";
        // "후불" | "선불" (기본정보 선택값을 자동등록용으로 동기화)
        public string TableMode { get; set; } = "";
    }
}
