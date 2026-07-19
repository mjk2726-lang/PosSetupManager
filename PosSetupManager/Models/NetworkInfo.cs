using System;

namespace PosSetupManager.Models
{
    [Serializable]
    public class NetworkInfo
    {
        public string RouterKT { get; set; } = "";
        public string RouterLG { get; set; } = "";
        public string RouterSK { get; set; } = "";
        public string RouterIpTime { get; set; } = "";
        public string RouterEtc { get; set; } = "";
        public string WifiAccount { get; set; } = "";
        public string MainPosInternalIP { get; set; } = "";
    }
}