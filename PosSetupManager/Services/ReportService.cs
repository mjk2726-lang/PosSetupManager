using System;
using System.IO;
using System.Text;
using PosSetupManager.Models;

namespace PosSetupManager.Services
{
    public class ReportService
    {
        public static void SaveReport(ChecklistData d)
        {
            try
            {
                var sb = new StringBuilder();
                var now = DateTime.Now;

                sb.AppendLine("========================================");
                sb.AppendLine("        POS Setup Manager 작업 보고서");
                sb.AppendLine("========================================");
                sb.AppendLine(string.Format("등록일시: {0}", now.ToString("yyyy-MM-dd HH:mm")));
                sb.AppendLine();

                sb.AppendLine("[ 기본정보 ]");
                sb.AppendLine(string.Format("매장명        : {0}", d.Basic.StoreName));
                sb.AppendLine(string.Format("설치 예정일   : {0} {1}", d.Basic.InstallDate.HasValue ? d.Basic.InstallDate.Value.ToString("yyyy-MM-dd") : "", d.Basic.InstallTime));
                sb.AppendLine(string.Format("원격 담당자   : {0}", d.Basic.RemoteManager));
                sb.AppendLine(string.Format("시작 시간     : {0}", d.Basic.StartTime));
                sb.AppendLine(string.Format("종료 시간     : {0}", d.Basic.EndTime));
                sb.AppendLine(string.Format("연동 종료시간 : {0}", d.Basic.LinkEndTime));
                sb.AppendLine(string.Format("소요 시간     : {0}", d.Basic.ElapsedTime));
                sb.AppendLine();

                sb.AppendLine("[ POS 설정 ]");
                sb.AppendLine(string.Format("원격 계정     : {0}", d.Pos.RemoteAccount));
                sb.AppendLine(string.Format("원격 어드민   : {0}", d.Pos.RemoteAdmin));
                sb.AppendLine(string.Format("LMM 계정      : {0}", d.Pos.LmmAccount));
                sb.AppendLine(string.Format("POS 종류      : {0}", string.Join(", ", d.Pos.PosTypes)));
                sb.AppendLine(string.Format("테이블 모드   : {0}", d.Pos.TableMode));
                sb.AppendLine();

                sb.AppendLine("[ 네트워크 ]");
                if (!string.IsNullOrEmpty(d.Network.RouterKT)) sb.AppendLine(string.Format("KT 공유기     : {0}", d.Network.RouterKT));
                if (!string.IsNullOrEmpty(d.Network.RouterLG)) sb.AppendLine(string.Format("LG 공유기     : {0}", d.Network.RouterLG));
                if (!string.IsNullOrEmpty(d.Network.RouterSK)) sb.AppendLine(string.Format("SK 공유기     : {0}", d.Network.RouterSK));
                if (!string.IsNullOrEmpty(d.Network.RouterIpTime)) sb.AppendLine(string.Format("ipTIME 공유기 : {0}", d.Network.RouterIpTime));
                if (!string.IsNullOrEmpty(d.Network.RouterEtc)) sb.AppendLine(string.Format("기타 공유기   : {0}", d.Network.RouterEtc));
                if (!string.IsNullOrEmpty(d.Network.WifiAccount)) sb.AppendLine(string.Format("와이파이 계정 : {0}", d.Network.WifiAccount));
                if (!string.IsNullOrEmpty(d.Network.MainPosInternalIP)) sb.AppendLine(string.Format("메인포스 IP   : {0}", d.Network.MainPosInternalIP));
                sb.AppendLine();

                sb.AppendLine("[ 기타 ]");
                sb.AppendLine(string.Format("쿠폰 생성여부 : {0}", d.Checklist.CheckCoupon));
                if (d.Checklist.CheckCoupon == "X" && !string.IsNullOrEmpty(d.Finish.CouponXReason))
                    sb.AppendLine(string.Format("쿠폰 X 사유   : {0}", d.Finish.CouponXReason));
                if (!string.IsNullOrEmpty(d.Finish.RemoteEduContact))
                    sb.AppendLine(string.Format("교육 연락처   : {0}", d.Finish.RemoteEduContact));
                if (!string.IsNullOrEmpty(d.Finish.InstallIssue))
                {
                    sb.AppendLine("설치 이슈:");
                    sb.AppendLine(d.Finish.InstallIssue);
                }

                sb.AppendLine();
                sb.AppendLine("========================================");

                var savePath = Forms.SettingsDialog.GetSavePath();
                var safeName = string.IsNullOrEmpty(d.Basic.StoreName) ? "매장" : d.Basic.StoreName;
                foreach (var c in Path.GetInvalidFileNameChars())
                    safeName = safeName.Replace(c, '_');
                var fileName = string.Format("{0}_{1}.txt", safeName, now.ToString("yyyyMMdd_HHmm"));
                File.WriteAllText(Path.Combine(savePath, fileName), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}