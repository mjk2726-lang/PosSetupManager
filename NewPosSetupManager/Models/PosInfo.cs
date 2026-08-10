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
        // "후불" | "선불" | "비연동"
        public string TableMode { get; set; } = "";
    }
}
