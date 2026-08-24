using Microsoft.Playwright;
using NewPosSetupManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NewPosSetupManager.Services
{
    public class PlaywrightService
    {
        public async Task<Tuple<bool, string>> RegisterAsync(ChecklistData d, IProgress<string> progress)
        {
            try
            {
                progress?.Report("브라우저 준비 중...");
                var page = await BrowserService.NewPageAsync();

                progress?.Report("다우오피스 접속 중...");
                var login = new LoginService();
                var loginResult = await login.EnsureLoggedInAsync(page);
                if (!loginResult.Item1) return loginResult;

                // 폼이 실제로 렌더링될 때까지 대기
                try
                {
                    await page.WaitForSelectorAsync(
                        "[data-cid='_zzv3du17w'] input[type='text'], [data-cid='_zzv3du17w'] input.txt",
                        new PageWaitForSelectorOptions { Timeout = 15000 });
                }
                catch
                {
                    return Tuple.Create(false, "폼 로드 시간 초과. 다우오피스 페이지를 확인해주세요.");
                }

                var input = new InputService(page);

                progress?.Report("기본 정보 입력 중...");
                var daouStoreName = string.IsNullOrEmpty(d.Basic.StoreId)
                    ? d.Basic.StoreName
                    : d.Basic.StoreName + " " + d.Basic.StoreId;
                await input.FillText("_zzv3du17w", daouStoreName);
                if (!string.IsNullOrEmpty(d.Basic.InstallDate) && DateTime.TryParse(d.Basic.InstallDate, out var installDateTime))
                {
                    await input.FillDate("_lwltls4xf", installDateTime, d.Basic.InstallTime);
                    var timeVal = await input.GetValue("_lwltls4xf", "input[data-time='start']");
                    System.Diagnostics.Debug.WriteLine("시간 값: " + timeVal);
                }
                await input.FillTime("_9zevls9rj", d.Basic.StartTime);
                await input.FillTime("_c4xt71s9q", d.Basic.EndTime);
                await input.FillTime("_ahda2xdtc", d.Basic.LinkEndTime);
                await input.FillRemoteManager(d.Basic.RemoteManager);

                await input.ClickByLabel("_n02xm18gn", d.Pos.RemoteAccount);
                await input.ClickByLabel("_hspl5ziik", d.Pos.RemoteAdmin == "동일" ? "O" : d.Pos.RemoteAdmin == "다름" ? "X" : "");
                await input.ClickByLabel("_moivpqu2t", d.Pos.LmmAccount);

                progress?.Report("POS 종류 선택 중...");
                await input.FillPosTypes(d.Pos.PosTypes);
                await input.ClickByLabel("_gl6wtnrv2", d.Pos.TableMode);

                await input.FillText("_pg8jw7hs3", d.Network.RouterKT);
                await input.FillText("_lnaltr7h8", d.Network.RouterLG);
                await input.FillText("_xa2oawfrf", d.Network.RouterSK);
                await input.FillText("_3wew05g63", d.Network.RouterIpTime);
                await input.FillText("_9gjn6trwf", d.Network.RouterEtc);
                await input.FillText("_ynyxmruux", d.Network.WifiAccount);
                await input.FillText("_pl37gn9u4", d.Network.MainPosInternalIP);

                progress?.Report("체크리스트 입력 중...");
                await input.FillOx("_x9b6k17mv", d.Checklist.CheckExternalIP);
                await input.FillOx("_yp0217xro", d.Checklist.CheckDHCP);
                await input.FillLocalMode(d.Checklist.LocalModeMenuBoard == "O", d.Checklist.LocalModeNoticeBoard == "O");
                await input.FillOx("_mh5liezmo", d.Checklist.CheckFirewall);
                await input.FillOx("_dmn3c8mu6", d.Checklist.CheckFirewallPopup);
                await input.FillText("_64h1g11ex", d.Checklist.OrderPosCount);
                await input.FillText("_p92n7dwzi", d.Checklist.OrderPosNote);
                await input.FillOx("_r2r9w8f8p", d.Checklist.CheckHiorderLogin);
                await input.FillOx("_0xsazai6o", d.Checklist.CheckSyncOrder);

                if (d.Pos.TableMode == "선불")
                    await input.FillPrepaid(d.Checklist.PrepaidTableCheck == "O", d.Checklist.PrepaidPaymentCheck == "O", d.Checklist.PrepaidOver5Man == "O", d.Checklist.PrepaidKSNET == "O");

                await input.FillOx("_zq9djt6dq", d.Checklist.CheckTableSort);
                await input.ClickByLabel("_llnvlcz2d", d.Checklist.WifiStatus);
                await input.FillOx("_cl79xc8mi", d.Checklist.CheckMenuImage);
                await input.FillOx("_4lkl8zq9u", d.Checklist.CheckNoticeBoardVer);
                await input.FillOx("_t5q3e3lwi", d.Checklist.CheckNoticeBoardAdmin);
                await input.FillOx("_348hrh4lz", d.Checklist.CheckMenuBoardVer);
                await input.FillOx("_ld1lsnkhg", d.Checklist.CheckMenuBoardAutoRun);
                await input.FillOx("_4ndziu3rn", d.Checklist.CheckIpMonitoringAutoRun);
                await input.FillOx("_pe91qn43l", d.Checklist.CheckCoupon);
                await input.ClickCheckboxByCid(
                    "_2u1cyr0yh", "혹시 현대옥 프랜차이즈인가요?", d.Checklist.HyundaiOkFranchise);
                if (d.Checklist.HyundaiOkFranchise == "O")
                    await input.ClickCheckboxByCid(
                        "_40qbjiiq1", "광고 미동의 여부 확인하셨나요?", d.Checklist.CheckAdOptOut);

                if (d.Checklist.CheckCoupon == "X")
                    await input.FillText("_cewz3pfou", d.Finish.CouponXReason);

                var rawEduContact = (d.Finish.RemoteEduContact ?? "").Trim();
                var hasValidEduContact = System.Text.RegularExpressions.Regex.IsMatch(
                    rawEduContact, @"^010-\d{4}-\d{4}$");
                // 전화번호가 아니더라도 "교육X", "점주 거부" 등의 사유는
                // 다우오피스 원격 교육 연락처 칸에 기록해 이력을 남긴다.
                if (!string.IsNullOrEmpty(rawEduContact))
                    await input.FillTextRaw("_1zbjr7ank", rawEduContact);
                await input.FillTextArea("_l3ylkbwy6", d.Finish.InstallIssue);

                if (d.Basic.AttachmentPaths != null && d.Basic.AttachmentPaths.Count > 0)
                {
                    progress?.Report("파일 첨부 중...");
                    await input.AttachFile(d.Basic.AttachmentPaths.ToArray());
                }

                progress?.Report("등록 버튼 클릭 중...");
                var submit = new SubmitService(page);
                var result = await submit.SubmitAsync();

                if (result.Item1)
                {
                    if (hasValidEduContact)
                    {
                        progress?.Report("교육지원 등록 중...");
                        var eduPage = await BrowserService.NewPageAsync();
                        var edu = new EduSupportService(eduPage);
                        var eduResult = await edu.RegisterAsync(d.Basic.StoreName);
                        if (eduResult.Item1)
                            progress?.Report("등록 완료! (다우오피스 + 교육지원)");
                        else
                            progress?.Report("다우오피스 등록 완료. 교육지원 등록 실패: " + eduResult.Item2);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(rawEduContact))
                            progress?.Report("교육 연락처 미 기입으로 교육등록 하지 않았습니다.");
                        else if (System.Text.RegularExpressions.Regex.IsMatch(rawEduContact, @"\d"))
                            progress?.Report("교육 연락처 형식이 올바르지 않아 교육등록 하지 않았습니다. 입력값: " + rawEduContact);
                        else
                            progress?.Report("교육지원 등록을 생략했습니다. 사유: " + rawEduContact);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, ex.Message);
            }
        }
    }
}
