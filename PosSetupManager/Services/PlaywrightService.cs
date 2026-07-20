using Microsoft.Playwright;
using PosSetupManager.Models;
using System;
using System.Threading.Tasks;

namespace PosSetupManager.Services
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

                var input = new InputService(page);

                progress?.Report("기본 정보 입력 중...");
                await input.FillText("_zzv3du17w", d.Basic.StoreName);
                if (d.Basic.InstallDate.HasValue)
                {
                    await input.FillDate("_lwltls4xf", d.Basic.InstallDate.Value, d.Basic.InstallTime);
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
                await input.FillLocalMode(d.Checklist.LocalModeMenuBoard, d.Checklist.LocalModeNoticeBoard);
                await input.FillOx("_mh5liezmo", d.Checklist.CheckFirewall);
                await input.FillOx("_dmn3c8mu6", d.Checklist.CheckFirewallPopup);
                await input.FillSelect("_64h1g11ex", d.Checklist.OrderPosCount);
                await input.FillText("_p92n7dwzi", d.Checklist.OrderPosNote);
                await input.FillOx("_r2r9w8f8p", d.Checklist.CheckHiorderLogin);
                await input.FillOx("_0xsazai6o", d.Checklist.CheckSyncOrder);

                if (d.Pos.TableMode == "선불")
                    await input.FillPrepaid(d.Checklist.PrepaidTableCheck, d.Checklist.PrepaidPaymentCheck, d.Checklist.PrepaidOver5Man, d.Checklist.PrepaidKSNET);

                await input.FillOx("_zq9djt6dq", d.Checklist.CheckTableSort);
                await input.ClickByLabel("_llnvlcz2d", d.Checklist.WifiStatus);
                await input.FillOx("_cl79xc8mi", d.Checklist.CheckMenuImage);
                await input.FillOx("_4lkl8zq9u", d.Checklist.CheckNoticeBoardVer);
                await input.FillOx("_t5q3e3lwi", d.Checklist.CheckNoticeBoardAdmin);
                await input.FillOx("_348hrh4lz", d.Checklist.CheckMenuBoardVer);
                await input.FillOx("_ld1lsnkhg", d.Checklist.CheckMenuBoardAutoRun);
                await input.FillOx("_pe91qn43l", d.Checklist.CheckCoupon);

                if (d.Checklist.CheckCoupon == "X")
                    await input.FillText("_cewz3pfou", d.Finish.CouponXReason);

                await input.FillText("_1zbjr7ank", d.Finish.RemoteEduContact);
                await input.FillTextArea("_l3ylkbwy6", d.Finish.InstallIssue);

                progress?.Report("등록 버튼 클릭 중...");
                var submit = new SubmitService(page);
                var result = await submit.SubmitAsync();

                // ── 파일 첨부 (등록 완료 후 에디터 클릭 → Ctrl+V → 재등록) ──
                if (result.Item1 && !string.IsNullOrEmpty(d.Basic.AttachmentPath))
                {
                    progress?.Report("파일 첨부 중...");
                    // 등록 후 페이지 전환 + 에디터 로드 대기
                    await page.WaitForTimeoutAsync(2000);
                    try
                    {
                        // id=dext_frame_editor 또는 등록 버튼이 다시 나타날 때까지 대기
                        await page.WaitForSelectorAsync(
                            "iframe[id='dext_frame_editor'], a.btn_major",
                            new PageWaitForSelectorOptions { Timeout = 10000 });
                    }
                    catch { await page.WaitForTimeoutAsync(3000); }
                    await page.WaitForTimeoutAsync(1500);

                    // 현재 모든 frame URL 로그
                    foreach (var f in page.Frames)
                        System.Diagnostics.Debug.WriteLine("FRAME: " + f.Name + " | " + f.Url);

                    await input.AttachFile(d.Basic.AttachmentPath);
                    await page.WaitForTimeoutAsync(1000);

                    progress?.Report("최종 등록 중...");
                    var submit2 = new SubmitService(page);
                    result = await submit2.SubmitAsync();
                }

                if (result.Item1)
                    progress?.Report("등록 완료! 브라우저에서 검수해주세요.");

                return result;
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, ex.Message);
            }
        }
    }
}