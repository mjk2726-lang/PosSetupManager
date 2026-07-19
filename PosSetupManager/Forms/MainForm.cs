using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PosSetupManager.Models;
using PosSetupManager.Services;

namespace PosSetupManager.Forms
{
    public class MainForm : Form
    {
        // ── 워크스페이스 ──
        private WorkspaceManager _workspace;
        private StoreSession _currentSession;

        // ── 레이아웃 ──
        private Panel pnlLeft;
        private Panel pnlRight;

        // ── 좌측 패널 컨트롤 ──
        private Panel pnlStoreList;
        private FluentButton btnNewStore;

        // ── 우측 탭 ──
        private Panel pnlTabs;
        private Panel[] tabPages;
        private FluentButton[] tabBtns;
        private int _currentTab = 0;
        private Label lblNoSession;

        // ── 기본정보 컨트롤 ──
        private TextBox txtStoreName, txtRemoteManager, txtStartTime, txtEndTime, txtLinkEndTime, txtElapsedTime, txtInstallTime;
        private DateTimePicker dtpInstallDate;
        private FluentButton btnStart, btnFinish;

        // ── POS 설정 컨트롤 ──
        private RadioButton rbLMM, rbChrome, rbSitrom, rbRemoteEtc;
        private RadioButton rbAdminSame, rbAdminDiff;
        private RadioButton rbLmmOk, rbLmmFail;
        private Dictionary<string, CheckBox> chkPosTypes = new Dictionary<string, CheckBox>();
        private RadioButton rbTablePost, rbTablePre, rbTableNone;
        private TextBox txtRouterKT, txtRouterLG, txtRouterSK, txtRouterIpTime, txtRouterEtc;
        private TextBox txtWifiAccount, txtMainPosIP;

        // ── 체크리스트 컨트롤 ──
        private OxRadio oxExternalIP, oxDHCP, oxFirewall, oxFirewallPopup;
        private CheckBox chkLocalMenuBoard, chkLocalNoticeBoard;
        private ComboBox cmbOrderPosCount;
        private TextBox txtOrderPosNote;
        private OxRadio oxHiorderLogin, oxSyncOrder;
        private Panel pnlPrepaid;
        private CheckBox chkPrepaidTable, chkPrepaidPayment, chkPrepaid5Man, chkPrepaidKSNET;
        private OxRadio oxTableSort;
        private RadioButton rbWifiGood, rbWifiOk, rbWifiBad;
        private OxRadio oxMenuImage, oxNoticeBoardVer, oxNoticeBoardAdmin;
        private OxRadio oxMenuBoardVer, oxMenuBoardAuto, oxCoupon;

        // ── 완료 컨트롤 ──
        private Panel pnlCouponXReason;
        private TextBox txtCouponXReason;
        private TextBox txtRemoteEduContact;
        private TextBox txtInstallIssue;
        private FluentButton btnRegister;

        private DateTime? workStartTime;
        private System.Windows.Forms.Timer _autoSaveTimer;

        public MainForm()
        {
            _workspace = new WorkspaceManager();
            InitializeComponent();
            RefreshStoreList();

            // 30초마다 자동저장
            _autoSaveTimer = new System.Windows.Forms.Timer();
            _autoSaveTimer.Interval = 30000;
            _autoSaveTimer.Tick += (s, e) => AutoSave();
            _autoSaveTimer.Start();

            // 이전 작업 복구
            if (_workspace.ActiveSessions.Count > 0)
                SelectSession(_workspace.ActiveSessions[0]);
        }

        private void InitializeComponent()
        {
            this.Text = "POS Setup Manager";
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1280, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = FluentColors.Background;
            this.Font = FluentFonts.Body;

            var leftPanel = new Panel
            {
                Width = 220,
                Dock = DockStyle.Left,
                BackColor = FluentColors.NavBg
            };
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = FluentColors.Background
            };

            // WinForms Dock 규칙: Fill 먼저, Left 나중에 추가
            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);

            BuildLeftPanel(leftPanel);
            BuildRightPanel(rightPanel);
        }

        // ════════════════════════════════════════
        // 레이아웃
        // ════════════════════════════════════════
        private void BuildLeftPanel(Panel container)
        {
            pnlLeft = container;
            pnlLeft.Padding = new Padding(8);

            // 새 매장 버튼
            btnNewStore = new FluentButton
            {
                Text = "➕  새 매장",
                IsPrimary = true,
                Dock = DockStyle.Top,
                Height = 40
            };
            btnNewStore.Click += BtnNewStore_Click;

            // 매장 목록 영역
            pnlStoreList = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            // 설정 버튼
            var btnAccount = new Label
            {
                Text = "⚙  설정",
                Font = FluentFonts.Caption,
                ForeColor = FluentColors.TextSecond,
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnAccount.Click += (s, e) =>
            {
                var dlg = new SettingsDialog();
                dlg.ShowDialog(pnlLeft.FindForm());
            };

            // 완료 내역 토글 (이제 항상 보이므로 제거)
            pnlLeft.Controls.Add(pnlStoreList);
            pnlLeft.Controls.Add(btnAccount);
            pnlLeft.Controls.Add(btnNewStore);
        }

        private void BuildRightPanel(Panel container)
        {
            pnlRight = container;

            // 탭 버튼 (Top) - 헤더 없이 탭만
            pnlTabs = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = FluentColors.Surface, Visible = false };
            pnlTabs.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(FluentColors.Divider), 0, pnlTabs.Height - 1, pnlTabs.Width, pnlTabs.Height - 1);

            string[] tabNames = { "기본정보", "POS 설정", "체크리스트", "완료" };
            tabBtns = new FluentButton[tabNames.Length];
            tabPages = new Panel[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                tabBtns[i] = new FluentButton { Text = tabNames[i], Location = new Point(16 + i * 120, 8), Width = 112, Height = 32, IsPrimary = false };
                tabBtns[i].Click += (s, e) => SwitchTab(idx);
                pnlTabs.Controls.Add(tabBtns[i]);
            }

            // 저장 버튼 (탭 우측)
            var btnSave = new FluentButton
            {
                Text = "💾  저장",
                IsPrimary = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Height = 32
            };
            btnSave.Width = 90;
            pnlTabs.Controls.Add(btnSave);
            pnlTabs.Resize += (s, e) => { btnSave.Location = new Point(pnlTabs.Width - btnSave.Width - 16, 8); };
            btnSave.Click += (s, e) =>
            {
                if (_currentSession == null)
                {
                    MessageBox.Show("선택된 매장이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                AutoSave();
                btnSave.Text = "✔  저장됨";
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (ts, te) => { btnSave.Text = "💾  저장"; t.Stop(); };
                t.Start();
            };
            pnlRight.Controls.Add(pnlTabs);

            // 콘텐츠 영역 (Fill)
            var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = FluentColors.Background };
            pnlRight.Controls.Add(pnlContent);

            lblNoSession = new Label
            {
                Text = "좌측에서 매장을 선택하거나\n새 매장을 추가해주세요.",
                Font = FluentFonts.Body,
                ForeColor = FluentColors.TextSecond,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlContent.Controls.Add(lblNoSession);

            BuildTabBasic(pnlContent);
            BuildTabPos(pnlContent);
            BuildTabChecklist(pnlContent);
            BuildTabFinish(pnlContent);
        }

        // ════════════════════════════════════════
        // 좌측 매장 목록
        // ════════════════════════════════════════
        private void RefreshStoreList()
        {
            pnlStoreList.Controls.Clear();
            int y = 8;

            // ── 진행중 ──
            var lblActive = new Label { Text = "진행중", Font = FluentFonts.Caption, ForeColor = FluentColors.TextSecond, Location = new Point(8, y), AutoSize = true };
            pnlStoreList.Controls.Add(lblActive); y += 20;

            foreach (var session in _workspace.ActiveSessions)
            {
                var s = session;
                var item = new StoreListItem(s)
                {
                    Location = new Point(0, y),
                    Width = pnlStoreList.ClientSize.Width > 8 ? pnlStoreList.ClientSize.Width - 4 : 200,
                    IsSelected = (_currentSession != null && _currentSession.Id == s.Id)
                };
                item.Click += (sender, e) => SelectSession(s);
                item.OnDelete += (id) =>
                {
                    _workspace.RemoveSession(id);
                    if (_currentSession?.Id == id) ClearSession();
                    RefreshStoreList();
                };
                item.OnComplete += (id) =>
                {
                    if (MessageBox.Show("완료 처리하시겠습니까?", "완료 처리", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    if (_currentSession?.Id == id) AutoSave();
                    _workspace.CompleteSession(id);
                    if (_currentSession?.Id == id) ClearSession();
                    RefreshStoreList();
                };
                pnlStoreList.Controls.Add(item);
                y += item.Height + 2;
            }

            // ── 완료 내역 ──
            y += 8;
            var divider = new Panel { Location = new Point(8, y), Size = new Size(pnlStoreList.Width - 16, 1), BackColor = FluentColors.Divider };
            pnlStoreList.Controls.Add(divider); y += 8;

            var lblDone = new Label { Text = "완료 내역", Font = FluentFonts.Caption, ForeColor = FluentColors.TextSecond, Location = new Point(8, y), AutoSize = true };
            pnlStoreList.Controls.Add(lblDone); y += 20;

            foreach (var session in _workspace.CompletedSessions)
            {
                var s = session;
                var item = new StoreListItem(s)
                {
                    Location = new Point(0, y),
                    Width = pnlStoreList.ClientSize.Width > 8 ? pnlStoreList.ClientSize.Width - 4 : 200,
                    IsSelected = (_currentSession != null && _currentSession.Id == s.Id)
                };
                item.Click += (sender, e) => SelectSession(s);
                item.OnDelete += (id) => { _workspace.CompletedSessions.RemoveAll(x => x.Id == id); _workspace.Save(); RefreshStoreList(); };
                item.OnRestore += (id) =>
                {
                    if (MessageBox.Show("진행중으로 변경하시겠습니까?", "상태 변경", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    var found = _workspace.CompletedSessions.Find(x => x.Id == id);
                    if (found != null)
                    {
                        found.Status = "작성중";
                        found.CompletedAt = null;
                        _workspace.CompletedSessions.Remove(found);
                        _workspace.ActiveSessions.Add(found);
                        _workspace.Save();
                        RefreshStoreList();
                    }
                };
                pnlStoreList.Controls.Add(item);
                y += item.Height + 2;
            }

            pnlStoreList.Height = y + 8;
        }

        private bool _isLoading = false;

        private void SelectSession(StoreSession session)
        {
            // 이전 세션 저장
            if (_currentSession != null && _currentSession.Id != session.Id)
            {
                SaveUIToSession(_currentSession);
                _workspace.Save();
            }

            _currentSession = session;
            _isLoading = true;
            LoadSessionToUI(session);
            _isLoading = false;

            lblNoSession.Visible = false;
            pnlTabs.Visible = true;
            foreach (var p in tabPages) if (p != null) p.Visible = false;
            SwitchTab(0);
            RefreshStoreList();
        }

        private void ClearSession()
        {
            _currentSession = null;
            lblNoSession.Visible = true;
            pnlTabs.Visible = false;
            foreach (var p in tabPages) if (p != null) p.Visible = false;
        }

        private void BtnNewStore_Click(object sender, EventArgs e)
        {
            var session = _workspace.AddSession();
            RefreshStoreList();
            SelectSession(session);
        }

        // ════════════════════════════════════════
        // 탭 전환
        // ════════════════════════════════════════
        private void SwitchTab(int idx)
        {
            _currentTab = idx;
            for (int i = 0; i < tabBtns.Length; i++)
            {
                tabBtns[i].IsPrimary = (i == idx);
                tabBtns[i].Invalidate();
            }
            foreach (var p in tabPages) if (p != null) p.Visible = false;
            if (tabPages[idx] != null) tabPages[idx].Visible = true;
        }

        private Panel MakeTabPage(Panel container, int idx)
        {
            var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = FluentColors.Background, Visible = false };
            container.Controls.Add(p);
            tabPages[idx] = p;
            return p;
        }

        private CardPanel MakeCard(Panel page, string title, ref int y, int x = 24)
        {
            if (!string.IsNullOrEmpty(title))
            {
                var lbl = new Label { Text = title, Font = FluentFonts.BodyBold, ForeColor = FluentColors.TextSecond, Location = new Point(x, y), AutoSize = true };
                page.Controls.Add(lbl);
                y += 24;
            }
            var card = new CardPanel
            {
                Location = new Point(x, y),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Width = page.ClientSize.Width > x + 48 ? page.ClientSize.Width - x - 24 : 600
            };
            page.Controls.Add(card);
            page.Resize += (s, e) => { card.Width = page.ClientSize.Width > x + 48 ? page.ClientSize.Width - x - 24 : 600; };
            return card;
        }

        // ════════════════════════════════════════
        // 탭1: 기본정보
        // ════════════════════════════════════════
        private void BuildTabBasic(Panel container)
        {
            var page = MakeTabPage(container, 0);
            int x = 24;
            int y = 20;

            var title = new Label { Text = "기본정보", Font = FluentFonts.Title, ForeColor = FluentColors.TextPrimary, Location = new Point(x, y), AutoSize = true };
            page.Controls.Add(title); y += 36;

            var card = MakeCard(page, "", ref y, x);
            int cy = 12;

            AddRow(card, "매장명", ref cy); txtStoreName = AddTB(card, ref cy);
            txtStoreName.TextChanged += (s, e) => { AutoSave(); RefreshStoreList(); };

            AddRow(card, "설치 예정일", ref cy);
            dtpInstallDate = new DateTimePicker { Location = new Point(12, cy), Width = 160, Format = DateTimePickerFormat.Short, Font = FluentFonts.Body };
            card.Controls.Add(dtpInstallDate);
            txtInstallTime = new TextBox { Location = new Point(180, cy), Width = 80, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            card.Controls.Add(txtInstallTime);
            cy += 34;

            AddRow(card, "원격 담당자", ref cy); txtRemoteManager = AddTB(card, ref cy);

            AddDivider(card, cy); cy += 16;

            AddRow(card, "설치 시작시간", ref cy); txtStartTime = AddTB(card, ref cy, 100);
            AddRow(card, "설치 종료시간", ref cy); txtEndTime = AddTB(card, ref cy, 100);
            AddRow(card, "연동 종료시간", ref cy); txtLinkEndTime = AddTB(card, ref cy, 100);
            AddRow(card, "소요시간", ref cy);
            txtElapsedTime = AddTB(card, ref cy, 100);
            txtElapsedTime.ReadOnly = true; txtElapsedTime.BackColor = FluentColors.Background;

            // 시간 자동 포맷 (1200 → 12:00)
            txtStartTime.Leave += (s, e) => { txtStartTime.Text = FormatTime(txtStartTime.Text); UpdateElapsedTime(); UpdateStartButton(); };
            txtEndTime.Leave += (s, e) => { txtEndTime.Text = FormatTime(txtEndTime.Text); UpdateElapsedTime(); UpdateStartButton(); };
            txtLinkEndTime.Leave += (s, e) => { txtLinkEndTime.Text = FormatTime(txtLinkEndTime.Text); };
            txtStartTime.TextChanged += (s, e) => { UpdateElapsedTime(); UpdateStartButton(); };
            txtEndTime.TextChanged += (s, e) => { UpdateElapsedTime(); UpdateStartButton(); };

            cy += 8; card.Height = cy + 16; y += card.Height + 16;

            var card2 = MakeCard(page, "작업 시간 기록", ref y);
            int cy2 = 12;
            btnStart = new FluentButton { Text = "▶  작업 시작", IsPrimary = true, Location = new Point(12, cy2), Width = 130 };
            btnStart.Click += BtnStart_Click;
            btnFinish = new FluentButton { Text = "■  작업 완료", IsPrimary = false, Location = new Point(152, cy2), Width = 130, Enabled = false };
            btnFinish.Click += BtnFinish_Click;
            card2.Controls.AddRange(new Control[] { btnStart, btnFinish });
            card2.Height = cy2 + 32 + 16;
        }

        // ════════════════════════════════════════
        // 탭2: POS 설정
        // ════════════════════════════════════════
        private void BuildTabPos(Panel container)
        {
            var page = MakeTabPage(container, 1);
            int y = 20;
            var title = new Label { Text = "POS 설정", Font = FluentFonts.Title, ForeColor = FluentColors.TextPrimary, Location = new Point(24, y), AutoSize = true };
            page.Controls.Add(title); y += 36;

            var c1 = MakeCard(page, "원격 계정", ref y); int cy = 12;
            rbLMM = AddRb(c1, "LMM", 12, cy); rbChrome = AddRb(c1, "크롬", 80, cy);
            rbSitrom = AddRb(c1, "씨트롬", 148, cy); rbRemoteEtc = AddRb(c1, "기타", 228, cy); cy += 30;
            GroupRadios(rbLMM, rbChrome, rbSitrom, rbRemoteEtc);
            c1.Height = cy + 12; y += c1.Height + 12;

            var c2 = MakeCard(page, "원격 어드민", ref y); cy = 12;
            rbAdminSame = AddRb(c2, "동일", 12, cy); rbAdminDiff = AddRb(c2, "다름", 80, cy); cy += 30;
            GroupRadios(rbAdminSame, rbAdminDiff);
            c2.Height = cy + 12; y += c2.Height + 12;

            var c3 = MakeCard(page, "LMM 계정", ref y); cy = 12;
            rbLmmOk = AddRb(c3, "O (완료)", 12, cy); rbLmmFail = AddRb(c3, "X (미완료)", 100, cy); cy += 30;
            GroupRadios(rbLmmOk, rbLmmFail);
            c3.Height = cy + 12; y += c3.Height + 12;

            var c4 = MakeCard(page, "POS 종류", ref y); cy = 12;
            string[] posNames = { "이지포스", "엠포스", "스마일포스", "오케이포스", "메이트포스", "배달포스", "K포스", "하이픈포스(개통불가)", "유니온포스", "키움포스", "팝스(웨이브포스)", "링크포스", "포스마스터(티페이)", "퍼스트포스", "에어포스", "토스포스", "포스메이커스(개통불가)", "윙스포스", "안시포스", "기타", "연동안함", "타밴", "우리밴" };
            int col = 0, row = 0;
            foreach (var name in posNames)
            {
                var chk = new CheckBox { Text = name, Location = new Point(12 + col * 160, cy + row * 28), AutoSize = true, Font = FluentFonts.Body };
                chkPosTypes[name] = chk; c4.Controls.Add(chk);
                col++; if (col >= 4) { col = 0; row++; }
            }
            cy += (int)Math.Ceiling(posNames.Length / 4.0) * 28 + 8;
            c4.Height = cy + 12; y += c4.Height + 12;

            var c5 = MakeCard(page, "테이블 모드", ref y); cy = 12;
            rbTablePost = AddRb(c5, "후불", 12, cy); rbTablePre = AddRb(c5, "선불", 70, cy); rbTableNone = AddRb(c5, "비연동", 128, cy); cy += 30;
            GroupRadios(rbTablePost, rbTablePre, rbTableNone);
            rbTablePre.CheckedChanged += (s, e) => { if (pnlPrepaid != null) pnlPrepaid.Visible = rbTablePre.Checked; };
            c5.Height = cy + 12; y += c5.Height + 12;

            var c6 = MakeCard(page, "공유기 관리자 계정", ref y); cy = 12;
            txtRouterKT = AddLabelRow(c6, "KT", ref cy); txtRouterLG = AddLabelRow(c6, "LG", ref cy);
            txtRouterSK = AddLabelRow(c6, "SK", ref cy); txtRouterIpTime = AddLabelRow(c6, "ipTIME", ref cy);
            txtRouterEtc = AddLabelRow(c6, "기타", ref cy);
            c6.Height = cy + 12; y += c6.Height + 12;

            var c7 = MakeCard(page, "네트워크", ref y); cy = 12;
            txtWifiAccount = AddLabelRow(c7, "와이파이 계정/비밀번호", ref cy, 200);
            txtMainPosIP = AddLabelRow(c7, "메인포스 내부 IP", ref cy, 160);
            c7.Height = cy + 12;
        }

        // ════════════════════════════════════════
        // 탭3: 체크리스트
        // ════════════════════════════════════════
        private void BuildTabChecklist(Panel container)
        {
            var page = MakeTabPage(container, 2);
            int y = 20;

            var title = new Label { Text = "설치 체크리스트", Font = FluentFonts.Title, ForeColor = FluentColors.TextPrimary, Location = new Point(24, y), AutoSize = true };
            page.Controls.Add(title); y += 40;

            // 일괄 O 선택 버튼
            var btnAllO = new FluentButton
            {
                Text = "✔  일괄 O 선택",
                IsPrimary = false,
                Location = new Point(24, y),
                Width = 130,
                Height = 30
            };
            btnAllO.Click += (s, e) =>
            {
                oxExternalIP.SetValue("O"); oxDHCP.SetValue("O");
                oxFirewall.SetValue("O"); oxFirewallPopup.SetValue("O");
                oxHiorderLogin.SetValue("O"); oxSyncOrder.SetValue("O");
                oxTableSort.SetValue("O"); oxMenuImage.SetValue("O");
                oxNoticeBoardVer.SetValue("O"); oxNoticeBoardAdmin.SetValue("O");
                oxMenuBoardVer.SetValue("O"); oxMenuBoardAuto.SetValue("O");
                oxCoupon.SetValue("O");
            };
            page.Controls.Add(btnAllO);
            y += 40;

            var card = MakeCard(page, "", ref y); int cy = 12;
            oxExternalIP = AddOx(card, "외부로부터 공인 IP 확인", ref cy);
            oxDHCP = AddOx(card, "DHCP 및 포트포워딩 설정", ref cy);

            // 로컬모드 (메뉴판/알림판 체크박스)
            card.Controls.Add(new Label { Text = "로컬모드", Location = new Point(12, cy + 4), AutoSize = true, Font = FluentFonts.Body, ForeColor = FluentColors.TextPrimary });
            chkLocalMenuBoard = new CheckBox { Text = "메뉴판(5060)", Location = new Point(120, cy + 2), AutoSize = true, Font = FluentFonts.Body };
            chkLocalNoticeBoard = new CheckBox { Text = "알림판(5070)", Location = new Point(240, cy + 2), AutoSize = true, Font = FluentFonts.Body };
            card.Controls.AddRange(new Control[] { chkLocalMenuBoard, chkLocalNoticeBoard }); cy += 32;

            oxFirewall = AddOx(card, "방화벽 & 디펜더 OFF", ref cy);
            oxFirewallPopup = AddOx(card, "방화벽 & 디펜더 팝업 알림 설정 OFF", ref cy);

            AddDivider(card, cy); cy += 16;
            card.Controls.Add(new Label { Text = "오더포스 갯수", Location = new Point(12, cy + 4), AutoSize = true, Font = FluentFonts.Body });
            cmbOrderPosCount = new ComboBox { Location = new Point(200, cy), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, Font = FluentFonts.Body };
            cmbOrderPosCount.Items.AddRange(new object[] { "X", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            cmbOrderPosCount.SelectedIndex = 0;
            card.Controls.Add(cmbOrderPosCount); cy += 32;
            card.Controls.Add(new Label { Text = "오더포스 특이사항", Location = new Point(12, cy + 4), AutoSize = true, Font = FluentFonts.Body });
            txtOrderPosNote = new TextBox { Location = new Point(200, cy), Width = 300, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            card.Controls.Add(txtOrderPosNote); cy += 32;

            AddDivider(card, cy); cy += 16;
            oxHiorderLogin = AddOx(card, "하이오더 포스 계정 로그인", ref cy);
            oxSyncOrder = AddOx(card, "동기화 & 주문 테스트 출력", ref cy);

            pnlPrepaid = new Panel { Location = new Point(12, cy), Size = new Size(640, 84), BackColor = Color.FromArgb(235, 244, 255), Visible = false };
            pnlPrepaid.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(FluentColors.Accent), new Rectangle(0, 0, pnlPrepaid.Width - 1, pnlPrepaid.Height - 1)); TextRenderer.DrawText(e.Graphics, "★ 선불 매장인 경우", FluentFonts.BodyBold, new Rectangle(8, 4, 300, 20), FluentColors.Accent, TextFormatFlags.Left); };
            chkPrepaidTable = new CheckBox { Text = "선후불 테이블 확인", Location = new Point(8, 26), AutoSize = true, Font = FluentFonts.Body };
            chkPrepaidPayment = new CheckBox { Text = "결제&취소 주문확인", Location = new Point(180, 26), AutoSize = true, Font = FluentFonts.Body };
            chkPrepaid5Man = new CheckBox { Text = "5만원이상 테스트 결제 진행", Location = new Point(340, 26), AutoSize = true, Font = FluentFonts.Body };
            chkPrepaidKSNET = new CheckBox { Text = "KSNET(카드결제 테스트진행)", Location = new Point(8, 54), AutoSize = true, Font = FluentFonts.Body };
            pnlPrepaid.Controls.AddRange(new Control[] { chkPrepaidTable, chkPrepaidPayment, chkPrepaid5Man, chkPrepaidKSNET });
            card.Controls.Add(pnlPrepaid); cy += 96;

            AddDivider(card, cy); cy += 16;
            oxTableSort = AddOx(card, "동기화 후 환경설정 테이블관리 → 테이블명 순으로 정렬", ref cy);
            card.Controls.Add(new Label { Text = "설치 후 와이파이 상태 점검", Location = new Point(12, cy + 4), AutoSize = true, Font = FluentFonts.Body });
            rbWifiGood = new RadioButton { Text = "좋음", Location = new Point(260, cy + 2), AutoSize = true, Font = FluentFonts.Body };
            rbWifiOk = new RadioButton { Text = "양호", Location = new Point(320, cy + 2), AutoSize = true, Font = FluentFonts.Body };
            rbWifiBad = new RadioButton { Text = "불량", Location = new Point(380, cy + 2), AutoSize = true, Font = FluentFonts.Body };
            GroupRadios(rbWifiGood, rbWifiOk, rbWifiBad);
            card.Controls.AddRange(new Control[] { rbWifiGood, rbWifiOk, rbWifiBad }); cy += 32;

            AddDivider(card, cy); cy += 16;
            oxMenuImage = AddOx(card, "메뉴 이미지 요청", ref cy);
            oxNoticeBoardVer = AddOx(card, "알림판 Ver 확인", ref cy);
            oxNoticeBoardAdmin = AddOx(card, "알림판 어드민 자동 업데이트 설정 확인", ref cy);
            oxMenuBoardVer = AddOx(card, "메뉴판 Ver 확인", ref cy);
            oxMenuBoardAuto = AddOx(card, "메뉴-1차 자동실행 설정", ref cy);
            oxCoupon = AddOx(card, "쿠폰 생성 여부", ref cy);
            oxCoupon.OnChanged += v => { if (pnlCouponXReason != null) pnlCouponXReason.Visible = (v == "X"); };
            card.Height = cy + 16;
        }

        // ════════════════════════════════════════
        // 탭4: 완료
        // ════════════════════════════════════════
        private void BuildTabFinish(Panel container)
        {
            var page = MakeTabPage(container, 3);
            int y = 20;
            var title = new Label { Text = "완료", Font = FluentFonts.Title, ForeColor = FluentColors.TextPrimary, Location = new Point(24, y), AutoSize = true };
            page.Controls.Add(title); y += 36;

            var card = MakeCard(page, "", ref y); int cy = 12;

            pnlCouponXReason = new Panel { Location = new Point(0, cy), Height = 40, Width = 640, Visible = false };
            pnlCouponXReason.Controls.Add(new Label { Text = "쿠폰생성 X 사유", Location = new Point(12, 10), AutoSize = true, Font = FluentFonts.Body });
            txtCouponXReason = new TextBox { Location = new Point(140, 8), Width = 340, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            pnlCouponXReason.Controls.Add(txtCouponXReason);
            card.Controls.Add(pnlCouponXReason); cy += 48;

            card.Controls.Add(new Label { Text = "원격교육 받으실 연락처", Location = new Point(12, cy + 4), AutoSize = true, Font = FluentFonts.Body });
            txtRemoteEduContact = new TextBox { Location = new Point(180, cy), Width = 240, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            card.Controls.Add(txtRemoteEduContact); cy += 36;

            AddDivider(card, cy); cy += 16;
            card.Controls.Add(new Label { Text = "설치 시 이슈", Location = new Point(12, cy), AutoSize = true, Font = FluentFonts.Body });
            cy += 22;
            txtInstallIssue = new TextBox { Location = new Point(12, cy), Width = 640, Height = 120, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            card.Controls.Add(txtInstallIssue); cy += 136;
            card.Height = cy + 16; y += card.Height + 20;

            btnRegister = new FluentButton { Text = "다우오피스 자동 등록", IsPrimary = true, Location = new Point(0, y), Width = 180, Height = 36 };
            btnRegister.Click += BtnRegister_Click;
            page.Controls.Add(btnRegister);
        }

        // ════════════════════════════════════════
        // UI ↔ 세션 데이터 동기화
        // ════════════════════════════════════════
        private void LoadSessionToUI(StoreSession session)
        {
            var d = session.Data;
            txtStoreName.Text = d.Basic.StoreName;
            if (d.Basic.InstallDate.HasValue) dtpInstallDate.Value = d.Basic.InstallDate.Value;
            txtInstallTime.Text = d.Basic.InstallTime;
            txtRemoteManager.Text = d.Basic.RemoteManager;
            txtStartTime.Text = d.Basic.StartTime;
            txtEndTime.Text = d.Basic.EndTime;
            txtLinkEndTime.Text = d.Basic.LinkEndTime;
            txtElapsedTime.Text = d.Basic.ElapsedTime;

            SetRadio(rbLMM, d.Pos.RemoteAccount == "LMM"); SetRadio(rbChrome, d.Pos.RemoteAccount == "크롬");
            SetRadio(rbSitrom, d.Pos.RemoteAccount == "씨트롬"); SetRadio(rbRemoteEtc, d.Pos.RemoteAccount == "기타");
            SetRadio(rbAdminSame, d.Pos.RemoteAdmin == "동일"); SetRadio(rbAdminDiff, d.Pos.RemoteAdmin == "다름");
            SetRadio(rbLmmOk, d.Pos.LmmAccount == "O"); SetRadio(rbLmmFail, d.Pos.LmmAccount == "X");
            foreach (var kv in chkPosTypes) kv.Value.Checked = d.Pos.PosTypes.Contains(kv.Key);
            SetRadio(rbTablePost, d.Pos.TableMode == "후불"); SetRadio(rbTablePre, d.Pos.TableMode == "선불"); SetRadio(rbTableNone, d.Pos.TableMode == "비연동");

            txtRouterKT.Text = d.Network.RouterKT; txtRouterLG.Text = d.Network.RouterLG;
            txtRouterSK.Text = d.Network.RouterSK; txtRouterIpTime.Text = d.Network.RouterIpTime;
            txtRouterEtc.Text = d.Network.RouterEtc;
            txtWifiAccount.Text = d.Network.WifiAccount; txtMainPosIP.Text = d.Network.MainPosInternalIP;

            oxExternalIP.SetValue(d.Checklist.CheckExternalIP); oxDHCP.SetValue(d.Checklist.CheckDHCP);
            chkLocalMenuBoard.Checked = d.Checklist.LocalModeMenuBoard;
            chkLocalNoticeBoard.Checked = d.Checklist.LocalModeNoticeBoard;
            oxFirewall.SetValue(d.Checklist.CheckFirewall); oxFirewallPopup.SetValue(d.Checklist.CheckFirewallPopup);
            cmbOrderPosCount.SelectedItem = d.Checklist.OrderPosCount ?? "X";
            txtOrderPosNote.Text = d.Checklist.OrderPosNote;
            oxHiorderLogin.SetValue(d.Checklist.CheckHiorderLogin); oxSyncOrder.SetValue(d.Checklist.CheckSyncOrder);
            chkPrepaidTable.Checked = d.Checklist.PrepaidTableCheck; chkPrepaidPayment.Checked = d.Checklist.PrepaidPaymentCheck;
            chkPrepaid5Man.Checked = d.Checklist.PrepaidOver5Man; chkPrepaidKSNET.Checked = d.Checklist.PrepaidKSNET;
            oxTableSort.SetValue(d.Checklist.CheckTableSort);
            SetRadio(rbWifiGood, d.Checklist.WifiStatus == "좋음"); SetRadio(rbWifiOk, d.Checklist.WifiStatus == "양호"); SetRadio(rbWifiBad, d.Checklist.WifiStatus == "불량");
            oxMenuImage.SetValue(d.Checklist.CheckMenuImage); oxNoticeBoardVer.SetValue(d.Checklist.CheckNoticeBoardVer);
            oxNoticeBoardAdmin.SetValue(d.Checklist.CheckNoticeBoardAdmin); oxMenuBoardVer.SetValue(d.Checklist.CheckMenuBoardVer);
            oxMenuBoardAuto.SetValue(d.Checklist.CheckMenuBoardAutoRun); oxCoupon.SetValue(d.Checklist.CheckCoupon);

            txtCouponXReason.Text = d.Finish.CouponXReason;
            txtRemoteEduContact.Text = d.Finish.RemoteEduContact;
            txtInstallIssue.Text = d.Finish.InstallIssue;

            if (pnlCouponXReason != null) pnlCouponXReason.Visible = d.Checklist.CheckCoupon == "X";
            if (pnlPrepaid != null) pnlPrepaid.Visible = d.Pos.TableMode == "선불";
        }

        private void AutoSave()
        {
            if (_currentSession == null || _isLoading) return;
            SaveUIToSession(_currentSession);
            _workspace.Save();
        }

        private void SaveUIToSession(StoreSession session)
        {
            var d = session.Data;
            d.Basic.StoreName = txtStoreName.Text;
            d.Basic.InstallDate = dtpInstallDate.Value;
            d.Basic.InstallTime = txtInstallTime.Text;
            d.Basic.RemoteManager = txtRemoteManager.Text;
            d.Basic.StartTime = txtStartTime.Text;
            d.Basic.EndTime = txtEndTime.Text;
            d.Basic.LinkEndTime = txtLinkEndTime.Text;
            d.Basic.ElapsedTime = txtElapsedTime.Text;

            d.Pos.RemoteAccount = rbLMM.Checked ? "LMM" : rbChrome.Checked ? "크롬" : rbSitrom.Checked ? "씨트롬" : rbRemoteEtc.Checked ? "기타" : "";
            d.Pos.RemoteAdmin = rbAdminSame.Checked ? "동일" : rbAdminDiff.Checked ? "다름" : "";
            d.Pos.LmmAccount = rbLmmOk.Checked ? "O" : rbLmmFail.Checked ? "X" : "";
            d.Pos.PosTypes.Clear();
            foreach (var kv in chkPosTypes) if (kv.Value.Checked) d.Pos.PosTypes.Add(kv.Key);
            d.Pos.TableMode = rbTablePost.Checked ? "후불" : rbTablePre.Checked ? "선불" : rbTableNone.Checked ? "비연동" : "";

            d.Network.RouterKT = txtRouterKT.Text; d.Network.RouterLG = txtRouterLG.Text;
            d.Network.RouterSK = txtRouterSK.Text; d.Network.RouterIpTime = txtRouterIpTime.Text;
            d.Network.RouterEtc = txtRouterEtc.Text;
            d.Network.WifiAccount = txtWifiAccount.Text; d.Network.MainPosInternalIP = txtMainPosIP.Text;

            d.Checklist.CheckExternalIP = oxExternalIP.Value; d.Checklist.CheckDHCP = oxDHCP.Value;
            d.Checklist.LocalModeMenuBoard = chkLocalMenuBoard.Checked;
            d.Checklist.LocalModeNoticeBoard = chkLocalNoticeBoard.Checked;
            d.Checklist.CheckFirewall = oxFirewall.Value; d.Checklist.CheckFirewallPopup = oxFirewallPopup.Value;
            d.Checklist.OrderPosCount = cmbOrderPosCount.SelectedItem?.ToString() ?? "X";
            d.Checklist.OrderPosNote = txtOrderPosNote.Text;
            d.Checklist.CheckHiorderLogin = oxHiorderLogin.Value; d.Checklist.CheckSyncOrder = oxSyncOrder.Value;
            d.Checklist.PrepaidTableCheck = chkPrepaidTable.Checked; d.Checklist.PrepaidPaymentCheck = chkPrepaidPayment.Checked;
            d.Checklist.PrepaidOver5Man = chkPrepaid5Man.Checked; d.Checklist.PrepaidKSNET = chkPrepaidKSNET.Checked;
            d.Checklist.CheckTableSort = oxTableSort.Value;
            d.Checklist.WifiStatus = rbWifiGood.Checked ? "좋음" : rbWifiOk.Checked ? "양호" : rbWifiBad.Checked ? "불량" : "";
            d.Checklist.CheckMenuImage = oxMenuImage.Value; d.Checklist.CheckNoticeBoardVer = oxNoticeBoardVer.Value;
            d.Checklist.CheckNoticeBoardAdmin = oxNoticeBoardAdmin.Value; d.Checklist.CheckMenuBoardVer = oxMenuBoardVer.Value;
            d.Checklist.CheckMenuBoardAutoRun = oxMenuBoardAuto.Value; d.Checklist.CheckCoupon = oxCoupon.Value;

            d.Finish.CouponXReason = txtCouponXReason.Text;
            d.Finish.RemoteEduContact = txtRemoteEduContact.Text;
            d.Finish.InstallIssue = txtInstallIssue.Text;
        }

        public ChecklistData GetData()
        {
            if (_currentSession == null) return new ChecklistData();
            SaveUIToSession(_currentSession);
            return _currentSession.Data;
        }

        // ════════════════════════════════════════
        // 이벤트
        // ════════════════════════════════════════
        private string FormatTime(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = input.Trim().Replace(":", "");

            if (input.Length == 1) input = "0" + input + "00"; // 9 → 0900
            if (input.Length == 2) input = input + "00";        // 12 → 1200
            if (input.Length == 3) input = "0" + input;         // 900 → 0900

            if (input.Length == 4)
            {
                var h = input.Substring(0, 2);
                var m = input.Substring(2, 2);
                int hi, mi;
                if (int.TryParse(h, out hi) && int.TryParse(m, out mi) && hi < 24 && mi < 60)
                    return string.Format("{0:D2}:{1:D2}", hi, mi);
            }
            return input;
        }

        private void UpdateElapsedTime()
        {
            if (_isLoading) return;
            try
            {
                var startStr = txtStartTime.Text.Trim();
                var endStr = txtEndTime.Text.Trim();
                if (string.IsNullOrEmpty(startStr) || string.IsNullOrEmpty(endStr)) return;

                // HH:mm 또는 H:mm 형식 모두 허용
                TimeSpan startTs, endTs;
                if (!TimeSpan.TryParse(startStr, out startTs)) return;
                if (!TimeSpan.TryParse(endStr, out endTs)) return;

                if (endTs < startTs) endTs = endTs.Add(TimeSpan.FromHours(24));
                int minutes = (int)(endTs - startTs).TotalMinutes;
                txtElapsedTime.Text = string.Format("{0}분", minutes);
            }
            catch { }
        }

        private void UpdateStartButton()
        {
            if (btnStart == null) return;
            // 시작/종료 시간이 직접 입력된 경우 버튼 비활성화
            bool hasManualTime = !string.IsNullOrEmpty(txtStartTime.Text) || !string.IsNullOrEmpty(txtEndTime.Text);
            if (hasManualTime && !workStartTime.HasValue)
                btnStart.Enabled = false;
            else if (!hasManualTime)
                btnStart.Enabled = true;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            workStartTime = DateTime.Now;
            txtStartTime.Text = workStartTime.Value.ToString("HH:mm");
            btnStart.Enabled = false; btnFinish.Enabled = true;
            AutoSave();
        }

        private void BtnFinish_Click(object sender, EventArgs e)
        {
            var end = DateTime.Now;
            txtEndTime.Text = end.ToString("HH:mm");
            if (workStartTime.HasValue)
                txtElapsedTime.Text = string.Format("{0}분", (int)(end - workStartTime.Value).TotalMinutes);
            btnFinish.Enabled = false;
            AutoSave();
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            SaveUIToSession(_currentSession);

            // 필수 항목 검증
            var errors = ValidateRequired(_currentSession.Data);
            if (errors.Count > 0)
            {
                MessageBox.Show("필수 항목을 입력해주세요:\n\n" + string.Join("\n", errors),
                    "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnRegister.Enabled = false;
            btnRegister.Text = "등록 중...";

            var svc = new PlaywrightService();
            var progress = new Progress<string>(msg =>
            {
                if (InvokeRequired) Invoke(new Action(() => btnRegister.Text = msg));
                else btnRegister.Text = msg;
            });

            var result = await svc.RegisterAsync(_currentSession.Data, progress);

            btnRegister.Enabled = true;
            btnRegister.Text = "다우오피스 자동 등록";

            if (result.Item1)
            {
                // txt 파일 저장
                ReportService.SaveReport(_currentSession.Data);

                var confirm = MessageBox.Show(
                    "다우오피스에 등록됐습니다.\n브라우저에서 내용을 검수한 후 완료 처리하시겠습니까?",
                    "등록 완료",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (confirm == DialogResult.Yes)
                {
                    _workspace.CompleteSession(_currentSession.Id);
                    ClearSession();
                    RefreshStoreList();
                }
            }
            else
            {
                MessageBox.Show("등록 실패:\n" + result.Item2, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private System.Collections.Generic.List<string> ValidateRequired(ChecklistData d)
        {
            var errors = new System.Collections.Generic.List<string>();
            if (string.IsNullOrEmpty(d.Basic.StoreName)) errors.Add("• 매장명");
            if (string.IsNullOrEmpty(d.Basic.StartTime)) errors.Add("• 설치 시작시간");
            if (string.IsNullOrEmpty(d.Basic.EndTime)) errors.Add("• 설치 종료시간");
            if (string.IsNullOrEmpty(d.Pos.RemoteAccount)) errors.Add("• 원격 계정");
            if (string.IsNullOrEmpty(d.Pos.RemoteAdmin)) errors.Add("• 원격 어드민");
            if (string.IsNullOrEmpty(d.Pos.LmmAccount)) errors.Add("• LMM 계정");
            if (d.Pos.PosTypes.Count < 2) errors.Add("• POS 종류 (2개 이상 선택)");
            if (string.IsNullOrEmpty(d.Pos.TableMode)) errors.Add("• 테이블 모드");
            if (string.IsNullOrEmpty(d.Checklist.CheckExternalIP)) errors.Add("• 외부로부터 공인 IP 확인");
            if (string.IsNullOrEmpty(d.Checklist.CheckDHCP)) errors.Add("• DHCP 및 포트포워딩 설정");
            if (string.IsNullOrEmpty(d.Checklist.CheckFirewall)) errors.Add("• 방화벽 & 디펜더 OFF");
            if (string.IsNullOrEmpty(d.Checklist.CheckFirewallPopup)) errors.Add("• 방화벽 & 디펜더 팝업 알림 설정 OFF");
            if (string.IsNullOrEmpty(d.Checklist.CheckHiorderLogin)) errors.Add("• 하이오더 포스 계정 로그인");
            if (string.IsNullOrEmpty(d.Checklist.CheckSyncOrder)) errors.Add("• 동기화 & 주문 테스트 출력");
            if (string.IsNullOrEmpty(d.Checklist.CheckTableSort)) errors.Add("• 동기화 후 테이블관리 정렬");
            if (string.IsNullOrEmpty(d.Checklist.WifiStatus)) errors.Add("• 설치 후 와이파이 상태 점검");
            if (string.IsNullOrEmpty(d.Checklist.CheckMenuImage)) errors.Add("• 메뉴 이미지 요청");
            if (string.IsNullOrEmpty(d.Checklist.CheckNoticeBoardVer)) errors.Add("• 알림판 Ver 확인");
            if (string.IsNullOrEmpty(d.Checklist.CheckNoticeBoardAdmin)) errors.Add("• 알림판 어드민 자동 업데이트 설정 확인");
            if (string.IsNullOrEmpty(d.Checklist.CheckMenuBoardVer)) errors.Add("• 메뉴판 Ver 확인");
            if (string.IsNullOrEmpty(d.Checklist.CheckMenuBoardAutoRun)) errors.Add("• 메뉴-1차 자동실행 설정");
            if (string.IsNullOrEmpty(d.Checklist.CheckCoupon)) errors.Add("• 쿠폰 생성 여부");
            return errors;
        }

        // ════════════════════════════════════════
        // 헬퍼
        // ════════════════════════════════════════
        private void AddRow(Control p, string text, ref int y)
        {
            p.Controls.Add(new Label { Text = text, Location = new Point(12, y), AutoSize = true, Font = FluentFonts.Caption, ForeColor = FluentColors.TextSecond });
            y += 20;
        }

        private TextBox AddTB(Control p, ref int y, int w = 300)
        {
            var tb = new TextBox { Location = new Point(12, y), Width = w, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(tb); y += 32; return tb;
        }

        private void AddDivider(Control p, int y)
        {
            p.Controls.Add(new Panel { Location = new Point(12, y), Size = new Size(p.Width - 48, 1), BackColor = FluentColors.Divider });
        }

        private RadioButton AddRb(Control p, string text, int x, int y)
        {
            var rb = new RadioButton { Text = text, Location = new Point(x, y), AutoSize = true, Font = FluentFonts.Body };
            p.Controls.Add(rb); return rb;
        }

        private TextBox AddLabelRow(Control p, string label, ref int y, int w = 200)
        {
            p.Controls.Add(new Label { Text = label, Location = new Point(12, y + 4), AutoSize = true, Font = FluentFonts.Body });
            var tb = new TextBox { Location = new Point(180, y), Width = w, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(tb); y += 30; return tb;
        }

        private OxRadio AddOx(Control p, string label, ref int y)
        {
            var ox = new OxRadio(label)
            {
                Location = new Point(0, y),
                Width = p.Width > 8 ? p.Width - 8 : 440,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            p.Controls.Add(ox);
            p.Resize += (s, e) => { ox.Width = p.Width > 8 ? p.Width - 8 : 440; };
            y += ox.Height + 2;
            return ox;
        }

        private void SetRadio(RadioButton rb, bool val) { rb.Checked = val; }

        private void GroupRadios(params RadioButton[] rbs)
        {
            if (rbs.Length == 0 || rbs[0].Parent == null) return;
            var parent = rbs[0].Parent;
            int minX = int.MaxValue, minY = int.MaxValue, maxR = 0, maxB = 0;
            foreach (var rb in rbs) { minX = Math.Min(minX, rb.Left); minY = Math.Min(minY, rb.Top); maxR = Math.Max(maxR, rb.Right); maxB = Math.Max(maxB, rb.Bottom); }
            var wrap = new Panel { Location = new Point(minX, minY), Size = new Size(maxR - minX + 4, maxB - minY + 4), BackColor = Color.Transparent };
            foreach (var rb in rbs) { parent.Controls.Remove(rb); rb.Location = new Point(rb.Left - minX, rb.Top - minY); wrap.Controls.Add(rb); }
            parent.Controls.Add(wrap);
        }
    }

    // ════════════════════════════════════════
    // 매장 목록 아이템
    // ════════════════════════════════════════
    public class StoreListItem : UserControl
    {
        public bool IsSelected { get; set; }
        public event Action<string> OnDelete;
        public event Action<string> OnComplete;
        public event Action<string> OnRestore;
        private StoreSession _session;

        public StoreListItem(StoreSession session)
        {
            _session = session;
            this.Height = 52;
            this.Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

            var btnDel = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9f),
                ForeColor = FluentColors.TextSecond,
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnDel.Location = new Point(this.Width - 24, 4);
            btnDel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnDel.Click += (s, e) =>
            {
                if (MessageBox.Show("삭제하시겠습니까?", "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    OnDelete?.Invoke(_session.Id);
            };
            this.Controls.Add(btnDel);

            if (session.Status != "완료")
            {
                var btnDone = new Label
                {
                    Text = "완료처리",
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = FluentColors.Accent,
                    Size = new Size(48, 18),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BorderStyle = BorderStyle.FixedSingle
                };
                btnDone.Location = new Point(this.Width - 72, 28);
                btnDone.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnDone.Click += (s, e) => { OnComplete?.Invoke(_session.Id); };
                this.Controls.Add(btnDone);
            }
            else
            {
                var btnRestore = new Label
                {
                    Text = "복구",
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = FluentColors.TextSecond,
                    Size = new Size(36, 18),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BorderStyle = BorderStyle.FixedSingle
                };
                btnRestore.Location = new Point(this.Width - 60, 28);
                btnRestore.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnRestore.Click += (s, e) => { OnRestore?.Invoke(_session.Id); };
                this.Controls.Add(btnRestore);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var r = new Rectangle(2, 2, Width - 4, Height - 4);

            if (IsSelected)
            {
                using (var path = RoundRect(r, 6))
                    e.Graphics.FillPath(new SolidBrush(FluentColors.NavSelected), path);
                e.Graphics.FillRectangle(new SolidBrush(FluentColors.Accent), new Rectangle(2, 10, 3, Height - 20));
            }

            // 상태 아이콘
            TextRenderer.DrawText(e.Graphics, _session.StatusIcon, FluentFonts.Body,
                new Rectangle(12, 4, 20, 20), FluentColors.TextPrimary, TextFormatFlags.Left);

            // 매장명
            TextRenderer.DrawText(e.Graphics, _session.DisplayName, FluentFonts.BodyBold,
                new Rectangle(32, 4, Width - 56, 22), FluentColors.TextPrimary, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            // 생성 시간
            TextRenderer.DrawText(e.Graphics, _session.CreatedAt.ToString("HH:mm"), FluentFonts.Caption,
                new Rectangle(32, 26, Width - 56, 18), FluentColors.TextSecond, TextFormatFlags.Left);
        }

        private System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ════════════════════════════════════════
    // OxRadio 커스텀 컨트롤
    // ════════════════════════════════════════
    public class OxRadio : UserControl
    {
        private RadioButton rbO, rbX;
        public event Action<string> OnChanged;

        public OxRadio(string labelText)
        {
            this.Height = 28;
            this.BackColor = Color.Transparent;
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(12, 5),
                AutoSize = false,
                Font = FluentFonts.Body,
                ForeColor = FluentColors.TextPrimary,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            rbO = new RadioButton { Text = "O", Width = 44, Font = FluentFonts.Body, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            rbX = new RadioButton { Text = "X", Width = 44, Font = FluentFonts.Body, Anchor = AnchorStyles.Right | AnchorStyles.Top };

            this.SizeChanged += (s, e) =>
            {
                lbl.Width = this.Width - 100;
                rbO.Location = new Point(this.Width - 88, 4);
                rbX.Location = new Point(this.Width - 44, 4);
            };

            rbO.CheckedChanged += (s, e) => { if (rbO.Checked) OnChanged?.Invoke("O"); };
            rbX.CheckedChanged += (s, e) => { if (rbX.Checked) OnChanged?.Invoke("X"); };
            this.Controls.AddRange(new Control[] { lbl, rbO, rbX });
        }

        public string Value => rbO.Checked ? "O" : rbX.Checked ? "X" : "";

        public void SetValue(string v)
        {
            rbO.Checked = v == "O";
            rbX.Checked = v == "X";
        }
    }
}