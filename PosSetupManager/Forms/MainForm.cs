using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PosSetupManager.Models;
using PosSetupManager.Services;

namespace PosSetupManager.Forms
{
    public class MainForm : Form
    {
        private WorkspaceManager _workspace;
        private StoreSession _currentSession;

        // 좌측
        private Panel pnlStoreList;
        private FluentButton btnNewStore;

        // 헤더
        private Panel pnlHeaderCard;
        private Label lblHName, lblHStatus, lblHPct;
        private ProgressBar2 hBar;
        private Label lblCDate, lblCManager, lblCElapsed;
        private FluentButton btnSave;

        // 탭
        private Panel pnlTabs;
        private Panel[] tabPages;
        private Label[] tabLabels;
        private Panel[] tabBars;
        private int _currentTab = 0;
        private Label lblNoSession;

        // 사이드바
        private Panel pnlSidebar;
        private StatusBadge[] sideBadges;
        private Panel[] sideCircles;
        private Label[] sideCircleLbls;

        // 기본정보
        private RoundTextBox txtStoreName, txtRemoteManager;
        private RoundTextBox txtStartTime, txtEndTime, txtLinkEndTime, txtElapsedTime, txtInstallTime;
        private RoundDateTimePicker dtpInstallDate;
        private FluentButton btnStart, btnFinish;

        // POS
        private RadioButton rbLMM, rbChrome, rbSitrom, rbRemoteEtc;
        private RadioButton rbAdminSame, rbAdminDiff;
        private RadioButton rbLmmOk, rbLmmFail;
        private Dictionary<string, CheckBox> chkPosTypes = new Dictionary<string, CheckBox>();
        private RadioButton rbTablePost, rbTablePre, rbTableNone;

        // 공유기/네트워크 (기존 + 체크리스트에서 이동)
        private RoundTextBox txtRouterKT, txtRouterLG, txtRouterSK, txtRouterIpTime, txtRouterEtc;
        private RoundTextBox txtWifiAccount, txtMainPosIP;
        private OxRadio oxExternalIP, oxDHCP;
        private CheckBox chkLocalMenuBoard, chkLocalNoticeBoard;

        // 체크리스트
        private OxRadio oxFirewall, oxFirewallPopup;
        private RoundComboBox cmbOrderPosCount;
        private RoundTextBox txtOrderPosNote;
        private OxRadio oxHiorderLogin, oxSyncOrder;
        private Panel pnlPrepaid;
        private CheckBox chkPrepaidTable, chkPrepaidPayment, chkPrepaid5Man, chkPrepaidKSNET;
        private OxRadio oxTableSort;
        private RadioButton rbWifiGood, rbWifiOk, rbWifiBad;
        private OxRadio oxMenuImage, oxNoticeBoardVer, oxNoticeBoardAdmin;
        private OxRadio oxMenuBoardVer, oxMenuBoardAuto, oxCoupon;

        // 완료
        private Panel pnlCouponX;
        private RoundTextBox txtCouponXReason, txtRemoteEduContact, txtInstallIssue;
        private FluentButton btnRegister;

        private DateTime? workStartTime;
        private System.Windows.Forms.Timer _autoSaveTimer;

        // ── 색상 (Win11 Fluent) ──
        static readonly Color BG = Color.FromArgb(243, 243, 243);
        static readonly Color SURFACE = Color.White;
        static readonly Color ACCENT = Color.FromArgb(0, 120, 212);
        static readonly Color ACCENT_L = Color.FromArgb(235, 246, 255);
        static readonly Color BORDER = Color.FromArgb(229, 229, 229);
        static readonly Color BORDER_S = Color.FromArgb(200, 210, 230);
        static readonly Color TXT = Color.FromArgb(28, 28, 28);
        static readonly Color TXT_S = Color.FromArgb(96, 96, 96);
        static readonly Color TXT_M = Color.FromArgb(160, 160, 160);
        static readonly Color SEL_BG = Color.FromArgb(239, 246, 255);
        static readonly Color SUCCESS = Color.FromArgb(16, 124, 16);
        static readonly Color WARN_BG = Color.FromArgb(255, 252, 240);
        static readonly Color WARN_BR = Color.FromArgb(200, 160, 0);

        public MainForm()
        {
            _workspace = new WorkspaceManager();
            InitializeComponent();
            RefreshStoreList();
            _autoSaveTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _autoSaveTimer.Tick += (s, e) => AutoSave();
            _autoSaveTimer.Start();
            if (_workspace.ActiveSessions.Count > 0)
                SelectSession(_workspace.ActiveSessions[0]);
        }

        private void InitializeComponent()
        {
            Text = "POS Setup Manager";
            Size = new Size(1320, 840);
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BG;
            Font = new Font("Segoe UI", 9.5f);

            // Fill → Left 순서
            var right = new Panel { Dock = DockStyle.Fill, BackColor = BG };
            var left = new Panel { Width = 300, Dock = DockStyle.Left, BackColor = SURFACE };
            left.Paint += (s, e) => e.Graphics.DrawLine(new Pen(BORDER), left.Width - 1, 0, left.Width - 1, left.Height);

            Controls.Add(right);
            Controls.Add(left);
            BuildLeft(left);
            BuildRight(right);
        }

        // ═══════════════════════════════
        // 좌측 패널
        // ═══════════════════════════════
        private void BuildLeft(Panel c)
        {
            // Bottom → Fill → Top 순
            var bot = new Panel { Dock = DockStyle.Bottom, Height = 108, BackColor = SURFACE, Padding = new Padding(16, 8, 16, 16) };
            btnNewStore = new FluentButton { Text = "＋  새 매장", IsPrimary = true, Dock = DockStyle.Top, Height = 44 };
            btnNewStore.Click += (s, e) => { var ss = _workspace.AddSession(); RefreshStoreList(); SelectSession(ss); };
            var btnSet = new FluentButton { Text = "⚙  설정", Dock = DockStyle.Bottom, Height = 36 };
            btnSet.Click += (s, e) => { new SettingsDialog().ShowDialog(this); };
            bot.Controls.Add(btnSet);
            bot.Controls.Add(btnNewStore);
            c.Controls.Add(bot);

            pnlStoreList = new Panel { Dock = DockStyle.Fill, BackColor = SURFACE, AutoScroll = true, Padding = new Padding(12, 8, 12, 8) };
            c.Controls.Add(pnlStoreList);

            // 상단 헤더
            var head = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = SURFACE, Padding = new Padding(16, 0, 16, 0) };
            head.Controls.Add(new Label { Text = "작업 목록", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            c.Controls.Add(head);
        }

        // ═══════════════════════════════
        // 우측 패널
        // ═══════════════════════════════
        private void BuildRight(Panel c)
        {
            var body = new Panel { Dock = DockStyle.Fill, BackColor = BG };
            c.Controls.Add(body);

            // 탭 (Top) 먼저
            pnlTabs = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = SURFACE, Visible = false };
            pnlTabs.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(BORDER), 0, pnlTabs.Height - 1, pnlTabs.Width, pnlTabs.Height - 1);
            c.Controls.Add(pnlTabs);
            BuildTabStrip();

            // 헤더 카드 (Top) 나중에 → 맨 위
            pnlHeaderCard = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = BG, Visible = false, Padding = new Padding(16, 12, 24, 12) };
            c.Controls.Add(pnlHeaderCard);
            BuildHeaderCard();

            // 콘텐츠
            var sidebar = new Panel { Width = 280, Dock = DockStyle.Right, BackColor = BG, Padding = new Padding(0, 16, 16, 16), Visible = false };
            pnlSidebar = sidebar;
            body.Controls.Add(sidebar);
            BuildSidebar();

            var content = new Panel { Dock = DockStyle.Fill, BackColor = BG };
            body.Controls.Add(content);

            lblNoSession = new Label { Text = "좌측에서 매장을 선택하거나\n새 매장을 추가해주세요.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = TXT_M, Font = new Font("Segoe UI", 10f) };
            content.Controls.Add(lblNoSession);

            tabPages = new Panel[5];
            BuildTabBasic(content);
            BuildTabPos(content);
            BuildTabNetwork(content);
            BuildTabChecklist(content);
            BuildTabFinish(content);
        }

        // ═══════════════════════════════
        // 헤더 카드
        // ═══════════════════════════════
        private void BuildHeaderCard()
        {
            // pnlHeaderCard 안에 직접 배치 (RoundPanel 없음 - 잘림 방지)
            pnlHeaderCard.BackColor = SURFACE;

            lblHName = new Label { Location = new Point(20, 10), AutoSize = true, Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = TXT };
            lblHStatus = new Label { Location = new Point(20, 44), AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = TXT_S };
            hBar = new ProgressBar2 { Location = new Point(20, 62), Width = 260, Height = 6 };
            lblHPct = new Label { Location = new Point(288, 57), AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = TXT_S };

            lblCDate = MakeChip("설치예정일");
            lblCManager = MakeChip("원격담당자");
            lblCElapsed = MakeChip("소요시간");

            btnSave = new FluentButton { Text = "💾  저장", IsPrimary = true, Width = 90, Height = 46, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            btnSave.Click += (s, e) =>
            {
                if (_currentSession == null) return;
                AutoSave(); btnSave.Text = "✔  저장됨";
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (ts, te) => { btnSave.Text = "💾  저장"; t.Stop(); };
                t.Start();
            };

            pnlHeaderCard.Controls.AddRange(new Control[] { lblHName, lblHStatus, hBar, lblHPct, lblCDate, lblCManager, lblCElapsed, btnSave });

            pnlHeaderCard.Resize += (s, e) =>
            {
                int w = pnlHeaderCard.Width;
                btnSave.Location = new Point(w - btnSave.Width - 16, 27);
                lblCElapsed.Location = new Point(btnSave.Left - lblCElapsed.Width - 10, 18);
                lblCManager.Location = new Point(lblCElapsed.Left - lblCManager.Width - 10, 18);
                lblCDate.Location = new Point(lblCManager.Left - lblCDate.Width - 10, 18);
            };
        }

        private Label MakeChip(string title)
        {
            var lbl = new Label
            {
                Size = new Size(126, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f),
                ForeColor = TXT_S,
                BackColor = SURFACE,
                Text = title + "\r\n-"
            };
            lbl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RR(new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1), 8))
                    e.Graphics.DrawPath(new Pen(BORDER), path);
            };
            return lbl;
        }


        private void SetChip(Label chip, string title, string val)
        {
            chip.Text = title + "\r\n" + val;
            chip.Invalidate();
        }

        // ═══════════════════════════════
        // 탭 스트립
        // ═══════════════════════════════
        private void BuildTabStrip()
        {
            string[] names = { "기본정보", "POS 설정", "공유기/네트워크", "체크리스트", "완료" };
            int[] ws = { 100, 100, 148, 112, 80 };
            tabLabels = new Label[5];
            tabBars = new Panel[5];
            int x = 16;
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var pnl = new Panel { Location = new Point(x, 0), Size = new Size(ws[i], 48), BackColor = SURFACE, Cursor = Cursors.Hand };
                var lbl = new Label { Text = names[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f), ForeColor = TXT_S, Cursor = Cursors.Hand };
                var bar = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Color.Transparent };
                pnl.Controls.Add(lbl);
                pnl.Controls.Add(bar);
                pnl.Click += (s, e) => SwitchTab(idx);
                lbl.Click += (s, e) => SwitchTab(idx);
                tabLabels[i] = lbl; tabBars[i] = bar;
                pnlTabs.Controls.Add(pnl);
                x += ws[i];
            }
        }

        // ═══════════════════════════════
        // 작업순서 사이드바
        // ═══════════════════════════════
        private void BuildSidebar()
        {
            var card = new RoundPanel(12) { Dock = DockStyle.Fill, BackColor = SURFACE };
            pnlSidebar.Controls.Add(card);

            // 제목 라벨 - 절대 위치
            var lblSideTitle = new Label { Text = "작업 순서", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = TXT, Location = new Point(20, 16), AutoSize = true };
            card.Controls.Add(lblSideTitle);

            // 스텝 컨테이너 - 절대 위치
            var sc = new Panel { Location = new Point(20, 52), BackColor = SURFACE };
            card.Controls.Add(sc);
            card.Resize += (s, e) => { sc.Size = new Size(card.Width - 40, card.Height - 60); };

            string[] names = { "기본정보", "POS 설정", "공유기/네트워크", "체크리스트", "완료" };
            sideBadges = new StatusBadge[5];
            sideCircles = new Panel[5];
            sideCircleLbls = new Label[5];

            sc.Paint += (s2, e2) =>
            {
                // 단계들 사이 점선 직접 그리기
                for (int i = 1; i < 5; i++)
                {
                    int lineX = 17;
                    int lineY = i * 56 - 10;
                    e2.Graphics.FillRectangle(new SolidBrush(BORDER), lineX, lineY, 2, 12);
                }
            };

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                int rowY = i * 56;

                var row = new Panel { Location = new Point(0, rowY), Size = new Size(200, 50), BackColor = SURFACE, Cursor = Cursors.Hand };
                sc.Resize += (s2, e2) => { row.Width = sc.Width; };

                // 번호 원
                var circ = new Panel { Location = new Point(0, 8), Size = new Size(34, 34), BackColor = Color.FromArgb(230, 230, 230) };
                circ.Paint += (s2, e2) =>
                {
                    e2.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e2.Graphics.FillEllipse(new SolidBrush(circ.BackColor), 0, 0, 33, 33);
                };
                var circLbl = new Label { Text = (i + 1).ToString(), Size = new Size(34, 34), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = TXT_S };
                circ.Controls.Add(circLbl);
                sideCircles[i] = circ;
                sideCircleLbls[i] = circLbl;

                var nameLbl = new Label { Text = names[i], Location = new Point(44, 14), AutoSize = true, Font = new Font("Segoe UI", 9.5f), ForeColor = TXT_S, Cursor = Cursors.Hand };
                var badge = new StatusBadge { Location = new Point(150, 14) };
                sideBadges[i] = badge;

                row.Controls.AddRange(new Control[] { circ, nameLbl, badge });
                row.Click += (s2, e2) => SwitchTab(idx);
                nameLbl.Click += (s2, e2) => SwitchTab(idx);
                sc.Controls.Add(row);
            }

            // 안내 문구
            var hint = new Label
            {
                Text = "💡 위에서부터 순서대로\n작업을 진행해주세요.",
                Dock = DockStyle.Bottom,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TXT_M,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            card.Controls.Add(hint);
        }

        // ═══════════════════════════════
        // 탭 전환
        // ═══════════════════════════════
        private void SwitchTab(int idx)
        {
            _currentTab = idx;
            for (int i = 0; i < 5; i++)
            {
                bool a = (i == idx);
                tabLabels[i].ForeColor = a ? ACCENT : TXT_S;
                tabLabels[i].Font = new Font("Segoe UI", 9.5f, a ? FontStyle.Bold : FontStyle.Regular);
                tabBars[i].BackColor = a ? ACCENT : Color.Transparent;
            }
            foreach (var p in tabPages) if (p != null) p.Visible = false;
            if (tabPages[idx] != null) tabPages[idx].Visible = true;
            RefreshSidebar();
        }

        private Panel MakePage(Panel c, int idx)
        {
            var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG, Visible = false };
            c.Controls.Add(p);
            tabPages[idx] = p;
            return p;
        }

        // ═══════════════════════════════
        // 카드 생성 헬퍼
        // ═══════════════════════════════
        private RoundPanel MakeCard(Panel page, ref int y, string title = "")
        {
            if (!string.IsNullOrEmpty(title))
            {
                var lbl = new Label { Text = title, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, y), AutoSize = true };
                page.Controls.Add(lbl);
                y += 28;
            }
            var card = new RoundPanel(12)
            {
                Location = new Point(0, y),
                BackColor = SURFACE,
                Padding = new Padding(20),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Width = page.ClientSize.Width > 20 ? page.ClientSize.Width : 700
            };
            page.Controls.Add(card);
            page.Resize += (s, e) => { card.Width = page.ClientSize.Width > 20 ? page.ClientSize.Width : 700; };
            y += 8;
            return card;
        }

        private Panel MakeScrollContent(Panel page)
        {
            var inner = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG, Padding = new Padding(20, 16, 20, 16) };
            page.Controls.Add(inner);
            return inner;
        }

        // ═══════════════════════════════
        // 탭1: 기본정보
        // ═══════════════════════════════
        private void BuildTabBasic(Panel c)
        {
            var page = MakePage(c, 0);
            var inner = MakeScrollContent(page);
            int y = 0;

            var card = MakeCard(inner, ref y);
            int cy = 0;

            card.Controls.Add(new Label { Text = "기본정보", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;

            // 행1: 매장명 / 설치예정일 / 설치예정시간
            AddFL(card, "매장명 *", 0, cy);
            AddFL(card, "설치 예정일 *", 220, cy);
            AddFL(card, "설치 예정 시간", 450, cy); cy += 22;

            txtStoreName = AddRTB(card, 0, cy, 204);
            txtStoreName.TextChanged += (s, e) => { AutoSave(); RefreshStoreList(); };
            dtpInstallDate = new RoundDateTimePicker { Location = new Point(220, cy), Width = 210, Height = 42, Format = DateTimePickerFormat.Short };
            card.Controls.Add(dtpInstallDate);
            txtInstallTime = AddRTB(card, 450, cy, 140); cy += 50;

            // 행2: 원격담당자
            AddFL(card, "원격 담당자", 0, cy); cy += 22;
            txtRemoteManager = AddRTB(card, 0, cy, 204); cy += 50;

            AddDivider(card, cy); cy += 20;

            // 행3: 시간
            AddFL(card, "작업 시작 시간 *", 0, cy);
            AddFL(card, "작업 종료 시간 *", 220, cy);
            AddFL(card, "연동 종료 시간", 450, cy); cy += 22;
            txtStartTime = AddRTB(card, 0, cy, 200);
            txtEndTime = AddRTB(card, 220, cy, 200);
            txtLinkEndTime = AddRTB(card, 450, cy, 160); cy += 50;

            AddFL(card, "소요시간 (자동계산)", 0, cy); cy += 22;
            txtElapsedTime = AddRTB(card, 0, cy, 160);
            txtElapsedTime.ReadOnly = true;
            txtElapsedTime.BackColor = Color.FromArgb(245, 245, 245); cy += 50;

            txtStartTime.Leave += (s, e) => { txtStartTime.Text = FmtT(txtStartTime.Text); UpdateElapsed(); UpdateStartBtn(); };
            txtEndTime.Leave += (s, e) => { txtEndTime.Text = FmtT(txtEndTime.Text); UpdateElapsed(); UpdateStartBtn(); };
            txtLinkEndTime.Leave += (s, e) => { txtLinkEndTime.Text = FmtT(txtLinkEndTime.Text); };
            txtStartTime.TextChanged += (s, e) => { UpdateElapsed(); UpdateStartBtn(); };
            txtEndTime.TextChanged += (s, e) => { UpdateElapsed(); UpdateStartBtn(); };

            card.Height = cy + 20; y += card.Height + 16;

            // 작업 시간 기록 카드
            var card2 = MakeCard(inner, ref y);
            int cy2 = 0;
            card2.Controls.Add(new Label { Text = "작업 시간 기록", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy2), AutoSize = true }); cy2 += 36;
            btnStart = new FluentButton { Text = "▶  작업 시작", Location = new Point(0, cy2), Width = 140, Height = 40 };
            btnFinish = new FluentButton { Text = "✔  작업 완료", Location = new Point(152, cy2), Width = 140, Height = 40, Enabled = false };
            btnStart.Click += BtnStart_Click;
            btnFinish.Click += BtnFinish_Click;
            card2.Controls.AddRange(new Control[] { btnStart, btnFinish });
            card2.Height = cy2 + 60; y += card2.Height + 16;

            // 경고바
            var warn = new RoundPanel(8) { Location = new Point(0, y), Height = 48, BackColor = WARN_BG, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            warn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RR(new Rectangle(0, 0, warn.Width - 1, warn.Height - 1), 8))
                    e.Graphics.DrawPath(new Pen(WARN_BR), path);
            };
            warn.Controls.Add(new Label { Text = "⚠  필수 입력 항목을 모두 입력해주세요.\r\n      * 표시 항목은 필수 입력 항목입니다.", Location = new Point(14, 6), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(130, 80, 0) });
            inner.Controls.Add(warn);
            inner.Resize += (s, e) => { warn.Width = inner.ClientSize.Width > 20 ? inner.ClientSize.Width : 600; };
        }

        // ═══════════════════════════════
        // 탭2: POS 설정
        // ═══════════════════════════════
        private void BuildTabPos(Panel c)
        {
            var page = MakePage(c, 1);
            var inner = MakeScrollContent(page);
            int y = 0;

            var c1 = MakeCard(inner, ref y); int cy = 0;
            c1.Controls.Add(new Label { Text = "원격 계정", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            rbLMM = AddRB(c1, "LMM", 0, cy); rbChrome = AddRB(c1, "크롬", 90, cy);
            rbSitrom = AddRB(c1, "씨트롬", 180, cy); rbRemoteEtc = AddRB(c1, "기타", 280, cy); cy += 34;
            GroupRBs(rbLMM, rbChrome, rbSitrom, rbRemoteEtc);
            c1.Height = cy + 20; y += c1.Height + 16;

            var c2 = MakeCard(inner, ref y); cy = 0;
            c2.Controls.Add(new Label { Text = "원격 어드민", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            rbAdminSame = AddRB(c2, "동일", 0, cy); rbAdminDiff = AddRB(c2, "다름", 90, cy); cy += 34;
            GroupRBs(rbAdminSame, rbAdminDiff);
            c2.Height = cy + 20; y += c2.Height + 16;

            var c3 = MakeCard(inner, ref y); cy = 0;
            c3.Controls.Add(new Label { Text = "LMM 계정", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            rbLmmOk = AddRB(c3, "O (완료)", 0, cy); rbLmmFail = AddRB(c3, "X (미완료)", 110, cy); cy += 34;
            GroupRBs(rbLmmOk, rbLmmFail);
            c3.Height = cy + 20; y += c3.Height + 16;

            var c4 = MakeCard(inner, ref y); cy = 0;
            c4.Controls.Add(new Label { Text = "POS 종류", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            string[] pns = { "이지포스", "엠포스", "스마일포스", "오케이포스", "메이트포스", "배달포스", "K포스", "하이픈포스(개통불가)", "유니온포스", "키움포스", "팝스(웨이브포스)", "링크포스", "포스마스터(티페이)", "퍼스트포스", "에어포스", "토스포스", "포스메이커스(개통불가)", "윙스포스", "안시포스", "기타", "연동안함", "타밴", "우리밴" };
            int col = 0, row2 = 0;
            foreach (var nm in pns)
            {
                var chk = new CheckBox { Text = nm, Location = new Point(col * 180, cy + row2 * 30), AutoSize = true, Font = FluentFonts.Body };
                chkPosTypes[nm] = chk; c4.Controls.Add(chk);
                col++; if (col >= 4) { col = 0; row2++; }
            }
            cy += (int)Math.Ceiling(pns.Length / 4.0) * 30 + 8;
            c4.Height = cy + 20; y += c4.Height + 16;

            var c5 = MakeCard(inner, ref y); cy = 0;
            c5.Controls.Add(new Label { Text = "테이블 모드", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            rbTablePost = AddRB(c5, "후불", 0, cy); rbTablePre = AddRB(c5, "선불", 90, cy); rbTableNone = AddRB(c5, "비연동", 180, cy); cy += 34;
            GroupRBs(rbTablePost, rbTablePre, rbTableNone);
            rbTablePre.CheckedChanged += (s, e) => { if (pnlPrepaid != null) pnlPrepaid.Visible = rbTablePre.Checked; };
            c5.Height = cy + 20;
        }

        // ═══════════════════════════════
        // 탭3: 공유기/네트워크
        // (기존 + 체크리스트에서 이동: 공인IP, DHCP, 로컬모드)
        // ═══════════════════════════════
        private void BuildTabNetwork(Panel c)
        {
            var page = MakePage(c, 2);
            var inner = MakeScrollContent(page);
            int y = 0;

            // ── 이동된 체크 항목 ──
            var c0 = MakeCard(inner, ref y); int cy = 0;
            c0.Controls.Add(new Label { Text = "네트워크 확인", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            oxExternalIP = AddOX(c0, "외부로부터 공인 IP 확인", ref cy);
            oxDHCP = AddOX(c0, "DHCP 및 포트포워딩 설정", ref cy);

            c0.Controls.Add(new Label { Text = "로컬모드", Location = new Point(0, cy + 6), AutoSize = true, Font = FluentFonts.Body });
            chkLocalMenuBoard = new CheckBox { Text = "메뉴판(5060)", Location = new Point(120, cy + 4), AutoSize = true, Font = FluentFonts.Body };
            chkLocalNoticeBoard = new CheckBox { Text = "알림판(5070)", Location = new Point(250, cy + 4), AutoSize = true, Font = FluentFonts.Body };
            c0.Controls.AddRange(new Control[] { chkLocalMenuBoard, chkLocalNoticeBoard }); cy += 36;
            c0.Height = cy + 20; y += c0.Height + 16;

            // ── 공유기 계정 ──
            var c1 = MakeCard(inner, ref y); cy = 0;
            c1.Controls.Add(new Label { Text = "공유기 관리자 계정", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            txtRouterKT = AddRLR(c1, "KT", ref cy);
            txtRouterLG = AddRLR(c1, "LG", ref cy);
            txtRouterSK = AddRLR(c1, "SK", ref cy);
            txtRouterIpTime = AddRLR(c1, "ipTIME", ref cy);
            txtRouterEtc = AddRLR(c1, "기타", ref cy);
            c1.Height = cy + 20; y += c1.Height + 16;

            // ── 네트워크 ──
            var c2 = MakeCard(inner, ref y); cy = 0;
            c2.Controls.Add(new Label { Text = "네트워크 정보", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;
            txtWifiAccount = AddRLR(c2, "와이파이 계정/비밀번호", ref cy, 230);
            txtMainPosIP = AddRLR(c2, "메인포스 내부 IP", ref cy, 190);
            c2.Height = cy + 20;
        }

        // ═══════════════════════════════
        // 탭4: 체크리스트
        // ═══════════════════════════════
        private void BuildTabChecklist(Panel c)
        {
            var page = MakePage(c, 3);
            var inner = MakeScrollContent(page);
            int y = 0;

            var btnAll = new FluentButton { Text = "✔  일괄 O 선택", Location = new Point(0, y), Width = 140, Height = 38 };
            btnAll.Click += (s, e) =>
            {
                oxFirewall.SetValue("O"); oxFirewallPopup.SetValue("O");
                oxHiorderLogin.SetValue("O"); oxSyncOrder.SetValue("O");
                oxTableSort.SetValue("O"); oxMenuImage.SetValue("O");
                oxNoticeBoardVer.SetValue("O"); oxNoticeBoardAdmin.SetValue("O");
                oxMenuBoardVer.SetValue("O"); oxMenuBoardAuto.SetValue("O");
                oxCoupon.SetValue("O");
            };
            inner.Controls.Add(btnAll); y += 54;

            var card = MakeCard(inner, ref y); int cy = 0;
            card.Controls.Add(new Label { Text = "설치 체크리스트", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;

            oxFirewall = AddOX(card, "방화벽 & 디펜더 OFF", ref cy);
            oxFirewallPopup = AddOX(card, "방화벽 & 디펜더 팝업 알림 설정 OFF", ref cy);

            AddDivider(card, cy); cy += 20;

            card.Controls.Add(new Label { Text = "오더포스 갯수", Location = new Point(0, cy + 6), AutoSize = true, Font = FluentFonts.Body });
            cmbOrderPosCount = new RoundComboBox { Location = new Point(180, cy), Width = 100, Height = 42 };
            cmbOrderPosCount.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbOrderPosCount.Items.AddRange(new object[] { "X", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            cmbOrderPosCount.SelectedIndex = 0;
            card.Controls.Add(cmbOrderPosCount); cy += 50;

            card.Controls.Add(new Label { Text = "오더포스 특이사항", Location = new Point(0, cy + 12), AutoSize = true, Font = FluentFonts.Body });
            txtOrderPosNote = new RoundTextBox { Location = new Point(180, cy), Width = 320, Height = 42 };
            card.Controls.Add(txtOrderPosNote); cy += 50;

            AddDivider(card, cy); cy += 20;

            oxHiorderLogin = AddOX(card, "하이오더 포스 계정 로그인", ref cy);
            oxSyncOrder = AddOX(card, "동기화 & 주문 테스트 출력", ref cy);

            pnlPrepaid = new Panel { Location = new Point(0, cy), Size = new Size(660, 88), BackColor = Color.FromArgb(235, 245, 255), Visible = false };
            pnlPrepaid.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RR(new Rectangle(0, 0, pnlPrepaid.Width - 1, pnlPrepaid.Height - 1), 8))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(235, 245, 255)), path);
                    e.Graphics.DrawPath(new Pen(ACCENT), path);
                }
                TextRenderer.DrawText(e.Graphics, "★ 선불 매장인 경우", new Font("Segoe UI", 9f, FontStyle.Bold), new Rectangle(12, 4, 300, 22), ACCENT, TextFormatFlags.Left);
            };
            chkPrepaidTable = new CheckBox { Text = "선후불 테이블 확인", Location = new Point(12, 28), AutoSize = true, Font = FluentFonts.Body };
            chkPrepaidPayment = new CheckBox { Text = "결제&취소 주문확인", Location = new Point(190, 28), AutoSize = true, Font = FluentFonts.Body };
            chkPrepaid5Man = new CheckBox { Text = "5만원이상 테스트 결제 진행", Location = new Point(360, 28), AutoSize = true, Font = FluentFonts.Body };
            chkPrepaidKSNET = new CheckBox { Text = "KSNET(카드결제 테스트진행)", Location = new Point(12, 58), AutoSize = true, Font = FluentFonts.Body };
            pnlPrepaid.Controls.AddRange(new Control[] { chkPrepaidTable, chkPrepaidPayment, chkPrepaid5Man, chkPrepaidKSNET });
            card.Controls.Add(pnlPrepaid); cy += 100;

            AddDivider(card, cy); cy += 20;

            oxTableSort = AddOX(card, "동기화 후 환경설정 테이블관리 → 테이블명 순으로 정렬", ref cy);
            card.Controls.Add(new Label { Text = "설치 후 와이파이 상태 점검", Location = new Point(0, cy + 6), AutoSize = true, Font = FluentFonts.Body });
            rbWifiGood = new RadioButton { Text = "좋음", Location = new Point(230, cy + 4), AutoSize = true, Font = FluentFonts.Body };
            rbWifiOk = new RadioButton { Text = "양호", Location = new Point(296, cy + 4), AutoSize = true, Font = FluentFonts.Body };
            rbWifiBad = new RadioButton { Text = "불량", Location = new Point(362, cy + 4), AutoSize = true, Font = FluentFonts.Body };
            GroupRBs(rbWifiGood, rbWifiOk, rbWifiBad);
            card.Controls.AddRange(new Control[] { rbWifiGood, rbWifiOk, rbWifiBad }); cy += 36;

            AddDivider(card, cy); cy += 20;

            oxMenuImage = AddOX(card, "메뉴 이미지 요청", ref cy);
            oxNoticeBoardVer = AddOX(card, "알림판 Ver 확인", ref cy);
            oxNoticeBoardAdmin = AddOX(card, "알림판 어드민 자동 업데이트 설정 확인", ref cy);
            oxMenuBoardVer = AddOX(card, "메뉴판 Ver 확인", ref cy);
            oxMenuBoardAuto = AddOX(card, "메뉴-1차 자동실행 설정", ref cy);
            oxCoupon = AddOX(card, "쿠폰 생성 여부", ref cy);
            oxCoupon.OnChanged += v => { if (pnlCouponX != null) pnlCouponX.Visible = (v == "X"); };
            card.Height = cy + 20;
        }

        // ═══════════════════════════════
        // 탭5: 완료
        // ═══════════════════════════════
        private void BuildTabFinish(Panel c)
        {
            var page = MakePage(c, 4);
            var inner = MakeScrollContent(page);
            int y = 0;

            var card = MakeCard(inner, ref y); int cy = 0;
            card.Controls.Add(new Label { Text = "완료", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TXT, Location = new Point(0, cy), AutoSize = true }); cy += 36;

            pnlCouponX = new Panel { Location = new Point(0, cy), Height = 42, Width = 660, Visible = false };
            pnlCouponX.Controls.Add(new Label { Text = "쿠폰생성 X 사유", Location = new Point(0, 10), AutoSize = true, Font = FluentFonts.Body });
            txtCouponXReason = new RoundTextBox { Location = new Point(150, 4), Width = 340, Height = 42 };
            pnlCouponX.Controls.Add(txtCouponXReason);
            card.Controls.Add(pnlCouponX); cy += 50;

            card.Controls.Add(new Label { Text = "원격교육 받으실 연락처", Location = new Point(0, cy + 12), AutoSize = true, Font = FluentFonts.Body });
            txtRemoteEduContact = new RoundTextBox { Location = new Point(200, cy), Width = 260, Height = 42 };
            card.Controls.Add(txtRemoteEduContact); cy += 50;

            AddDivider(card, cy); cy += 20;
            card.Controls.Add(new Label { Text = "설치 시 이슈", Location = new Point(0, cy), AutoSize = true, Font = FluentFonts.Body }); cy += 24;
            txtInstallIssue = new RoundTextBox(true) { Location = new Point(0, cy), Width = 660, Height = 120 };
            card.Controls.Add(txtInstallIssue); cy += 136;
            card.Height = cy + 20; y += card.Height + 20;

            btnRegister = new FluentButton { Text = "🌐  다우오피스 자동 등록", IsPrimary = true, Location = new Point(0, y), Width = 210, Height = 46 };
            btnRegister.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRegister.Click += BtnRegister_Click;
            inner.Controls.Add(btnRegister);
        }

        // ═══════════════════════════════
        // 진행률 계산
        // ═══════════════════════════════
        private void CalcProg(ChecklistData d, out int comp, out int tot)
        {
            tot = 21; comp = 0;
            if (!string.IsNullOrEmpty(d.Basic.StoreName)) comp++;
            if (!string.IsNullOrEmpty(d.Basic.StartTime)) comp++;
            if (!string.IsNullOrEmpty(d.Basic.EndTime)) comp++;
            if (!string.IsNullOrEmpty(d.Pos.RemoteAccount)) comp++;
            if (!string.IsNullOrEmpty(d.Pos.RemoteAdmin)) comp++;
            if (!string.IsNullOrEmpty(d.Pos.LmmAccount)) comp++;
            if (d.Pos.PosTypes.Count >= 2) comp++;
            if (!string.IsNullOrEmpty(d.Pos.TableMode)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckDHCP)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckExternalIP)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckFirewall)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckHiorderLogin)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckSyncOrder)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckTableSort)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.WifiStatus)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckMenuImage)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckNoticeBoardVer)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckNoticeBoardAdmin)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckMenuBoardVer)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckMenuBoardAutoRun)) comp++;
            if (!string.IsNullOrEmpty(d.Checklist.CheckCoupon)) comp++;
        }

        private void RefreshHeader()
        {
            if (_currentSession == null) return;
            var d = _currentSession.Data;
            lblHName.Text = string.IsNullOrEmpty(d.Basic.StoreName) ? "(매장명 미입력)" : d.Basic.StoreName;
            lblHStatus.Text = _currentSession.Status == "완료" ? "● 완료" : "● 작성중";
            lblHStatus.ForeColor = _currentSession.Status == "완료" ? SUCCESS : Color.FromArgb(160, 100, 0);
            int comp, tot; CalcProg(d, out comp, out tot);
            int pct = tot > 0 ? (int)Math.Round(comp * 100.0 / tot) : 0;
            hBar.Maximum = tot; hBar.Value = comp;
            lblHPct.Text = string.Format("{0}%   {1} / {2} 완료", pct, comp, tot);
            SetChip(lblCDate, "설치예정일", d.Basic.InstallDate.HasValue ? d.Basic.InstallDate.Value.ToString("yyyy-MM-dd") : "-");
            SetChip(lblCManager, "원격담당자", string.IsNullOrEmpty(d.Basic.RemoteManager) ? "-" : d.Basic.RemoteManager);
            SetChip(lblCElapsed, "소요시간", string.IsNullOrEmpty(d.Basic.ElapsedTime) ? "-" : d.Basic.ElapsedTime);
        }

        private void RefreshSidebar()
        {
            if (_currentSession == null || sideBadges == null) return;
            var d = _currentSession.Data;
            bool[] done = new bool[5];
            done[0] = !string.IsNullOrEmpty(d.Basic.StoreName) && !string.IsNullOrEmpty(d.Basic.StartTime) && !string.IsNullOrEmpty(d.Basic.EndTime);
            done[1] = !string.IsNullOrEmpty(d.Pos.RemoteAccount) && !string.IsNullOrEmpty(d.Pos.RemoteAdmin) && !string.IsNullOrEmpty(d.Pos.LmmAccount) && d.Pos.PosTypes.Count >= 2 && !string.IsNullOrEmpty(d.Pos.TableMode);
            done[2] = !string.IsNullOrEmpty(d.Checklist.CheckDHCP) && !string.IsNullOrEmpty(d.Checklist.CheckExternalIP);
            done[3] = !string.IsNullOrEmpty(d.Checklist.CheckFirewall) && !string.IsNullOrEmpty(d.Checklist.CheckHiorderLogin) && !string.IsNullOrEmpty(d.Checklist.CheckCoupon);
            done[4] = _currentSession.Status == "완료";
            for (int i = 0; i < 5; i++)
            {
                bool active = (i == _currentTab);
                if (done[i]) { sideBadges[i].Status = StatusBadge.BadgeStatus.Done; sideCircles[i].BackColor = ACCENT; sideCircleLbls[i].ForeColor = Color.White; }
                else if (active) { sideBadges[i].Status = StatusBadge.BadgeStatus.InProgress; sideCircles[i].BackColor = ACCENT; sideCircleLbls[i].ForeColor = Color.White; }
                else { sideBadges[i].Status = StatusBadge.BadgeStatus.Waiting; sideCircles[i].BackColor = Color.FromArgb(230, 230, 230); sideCircleLbls[i].ForeColor = TXT_S; }
                sideCircles[i].Invalidate();
            }
        }

        // ═══════════════════════════════
        // 매장 목록 갱신
        // ═══════════════════════════════
        private void RefreshStoreList()
        {
            pnlStoreList.Controls.Clear();
            int y = 4;

            // 진행중
            foreach (var s in _workspace.ActiveSessions)
            {
                var ss = s; int comp, tot; CalcProg(ss.Data, out comp, out tot);
                int pct = tot > 0 ? (int)Math.Round(comp * 100.0 / tot) : 0;
                var item = new TaskCard(ss, pct)
                {
                    Location = new Point(0, y),
                    Width = pnlStoreList.ClientSize.Width > 8 ? pnlStoreList.ClientSize.Width - 8 : 260,
                    IsSelected = (_currentSession?.Id == ss.Id)
                };
                item.Click += (sndr, e) => SelectSession(ss);
                item.OnDelete += id => { _workspace.RemoveSession(id); if (_currentSession?.Id == id) ClearSession(); RefreshStoreList(); };
                item.OnComplete += id =>
                {
                    if (MessageBox.Show("완료 처리하시겠습니까?", "완료", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    if (_currentSession?.Id == id) AutoSave();
                    _workspace.CompleteSession(id);
                    if (_currentSession?.Id == id) ClearSession();
                    RefreshStoreList();
                };
                pnlStoreList.Controls.Add(item); y += item.Height + 8;
            }

            // 완료 구분선
            y += 4;
            pnlStoreList.Controls.Add(new Label { Text = "완료", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = TXT_M, Location = new Point(4, y), AutoSize = true }); y += 22;

            foreach (var s in _workspace.CompletedSessions)
            {
                var ss = s; int comp, tot; CalcProg(ss.Data, out comp, out tot);
                int pct = tot > 0 ? (int)Math.Round(comp * 100.0 / tot) : 0;
                var item = new TaskCard(ss, pct)
                {
                    Location = new Point(0, y),
                    Width = pnlStoreList.ClientSize.Width > 8 ? pnlStoreList.ClientSize.Width - 8 : 260,
                    IsSelected = (_currentSession?.Id == ss.Id)
                };
                item.Click += (sndr, e) => SelectSession(ss);
                item.OnDelete += id => { _workspace.CompletedSessions.RemoveAll(x => x.Id == id); _workspace.Save(); RefreshStoreList(); };
                item.OnRestore += id =>
                {
                    if (MessageBox.Show("진행중으로 변경하시겠습니까?", "상태 변경", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    var f = _workspace.CompletedSessions.Find(x => x.Id == id);
                    if (f != null) { f.Status = "작성중"; f.CompletedAt = null; _workspace.CompletedSessions.Remove(f); _workspace.ActiveSessions.Add(f); _workspace.Save(); RefreshStoreList(); }
                };
                pnlStoreList.Controls.Add(item); y += item.Height + 8;
            }
        }

        private bool _isLoading = false;

        private void SelectSession(StoreSession session)
        {
            if (_currentSession != null && _currentSession.Id != session.Id) { Save2Ses(_currentSession); _workspace.Save(); }
            _currentSession = session;
            _isLoading = true; Load2UI(session); _isLoading = false;
            lblNoSession.Visible = false;
            pnlHeaderCard.Visible = pnlTabs.Visible = pnlSidebar.Visible = true;
            foreach (var p in tabPages) if (p != null) p.Visible = false;
            SwitchTab(0); RefreshHeader(); RefreshStoreList();
        }

        private void ClearSession()
        {
            _currentSession = null;
            lblNoSession.Visible = true;
            pnlHeaderCard.Visible = pnlTabs.Visible = pnlSidebar.Visible = false;
            foreach (var p in tabPages) if (p != null) p.Visible = false;
        }

        // ═══════════════════════════════
        // 데이터 동기화
        // ═══════════════════════════════
        private void Load2UI(StoreSession ss)
        {
            var d = ss.Data;
            txtStoreName.Text = d.Basic.StoreName;
            if (d.Basic.InstallDate.HasValue) dtpInstallDate.Inner.Value = d.Basic.InstallDate.Value;
            txtInstallTime.Text = d.Basic.InstallTime; txtRemoteManager.Text = d.Basic.RemoteManager;
            txtStartTime.Text = d.Basic.StartTime; txtEndTime.Text = d.Basic.EndTime;
            txtLinkEndTime.Text = d.Basic.LinkEndTime; txtElapsedTime.Text = d.Basic.ElapsedTime;

            SR(rbLMM, d.Pos.RemoteAccount == "LMM"); SR(rbChrome, d.Pos.RemoteAccount == "크롬");
            SR(rbSitrom, d.Pos.RemoteAccount == "씨트롬"); SR(rbRemoteEtc, d.Pos.RemoteAccount == "기타");
            SR(rbAdminSame, d.Pos.RemoteAdmin == "동일"); SR(rbAdminDiff, d.Pos.RemoteAdmin == "다름");
            SR(rbLmmOk, d.Pos.LmmAccount == "O"); SR(rbLmmFail, d.Pos.LmmAccount == "X");
            foreach (var kv in chkPosTypes) kv.Value.Checked = d.Pos.PosTypes.Contains(kv.Key);
            SR(rbTablePost, d.Pos.TableMode == "후불"); SR(rbTablePre, d.Pos.TableMode == "선불"); SR(rbTableNone, d.Pos.TableMode == "비연동");

            txtRouterKT.Text = d.Network.RouterKT; txtRouterLG.Text = d.Network.RouterLG;
            txtRouterSK.Text = d.Network.RouterSK; txtRouterIpTime.Text = d.Network.RouterIpTime; txtRouterEtc.Text = d.Network.RouterEtc;
            txtWifiAccount.Text = d.Network.WifiAccount; txtMainPosIP.Text = d.Network.MainPosInternalIP;

            oxExternalIP.SetValue(d.Checklist.CheckExternalIP); oxDHCP.SetValue(d.Checklist.CheckDHCP);
            chkLocalMenuBoard.Checked = d.Checklist.LocalModeMenuBoard; chkLocalNoticeBoard.Checked = d.Checklist.LocalModeNoticeBoard;
            oxFirewall.SetValue(d.Checklist.CheckFirewall); oxFirewallPopup.SetValue(d.Checklist.CheckFirewallPopup);
            cmbOrderPosCount.SelectedItem = d.Checklist.OrderPosCount ?? "X"; if (cmbOrderPosCount.SelectedIndex < 0) cmbOrderPosCount.SelectedIndex = 0; txtOrderPosNote.Text = d.Checklist.OrderPosNote;
            oxHiorderLogin.SetValue(d.Checklist.CheckHiorderLogin); oxSyncOrder.SetValue(d.Checklist.CheckSyncOrder);
            chkPrepaidTable.Checked = d.Checklist.PrepaidTableCheck; chkPrepaidPayment.Checked = d.Checklist.PrepaidPaymentCheck;
            chkPrepaid5Man.Checked = d.Checklist.PrepaidOver5Man; chkPrepaidKSNET.Checked = d.Checklist.PrepaidKSNET;
            oxTableSort.SetValue(d.Checklist.CheckTableSort);
            SR(rbWifiGood, d.Checklist.WifiStatus == "좋음"); SR(rbWifiOk, d.Checklist.WifiStatus == "양호"); SR(rbWifiBad, d.Checklist.WifiStatus == "불량");
            oxMenuImage.SetValue(d.Checklist.CheckMenuImage); oxNoticeBoardVer.SetValue(d.Checklist.CheckNoticeBoardVer);
            oxNoticeBoardAdmin.SetValue(d.Checklist.CheckNoticeBoardAdmin); oxMenuBoardVer.SetValue(d.Checklist.CheckMenuBoardVer);
            oxMenuBoardAuto.SetValue(d.Checklist.CheckMenuBoardAutoRun); oxCoupon.SetValue(d.Checklist.CheckCoupon);
            txtCouponXReason.Text = d.Finish.CouponXReason; txtRemoteEduContact.Text = d.Finish.RemoteEduContact; txtInstallIssue.Text = d.Finish.InstallIssue;
            if (pnlCouponX != null) pnlCouponX.Visible = d.Checklist.CheckCoupon == "X";
            if (pnlPrepaid != null) pnlPrepaid.Visible = d.Pos.TableMode == "선불";
        }

        private void AutoSave()
        {
            if (_currentSession == null || _isLoading) return;
            Save2Ses(_currentSession); _workspace.Save(); RefreshHeader(); RefreshSidebar();
        }

        private void Save2Ses(StoreSession ss)
        {
            var d = ss.Data;
            d.Basic.StoreName = txtStoreName.Text; d.Basic.InstallDate = dtpInstallDate.Inner.Value;
            d.Basic.InstallTime = txtInstallTime.Text; d.Basic.RemoteManager = txtRemoteManager.Text;
            d.Basic.StartTime = txtStartTime.Text; d.Basic.EndTime = txtEndTime.Text;
            d.Basic.LinkEndTime = txtLinkEndTime.Text; d.Basic.ElapsedTime = txtElapsedTime.Text;
            d.Pos.RemoteAccount = rbLMM.Checked ? "LMM" : rbChrome.Checked ? "크롬" : rbSitrom.Checked ? "씨트롬" : rbRemoteEtc.Checked ? "기타" : "";
            d.Pos.RemoteAdmin = rbAdminSame.Checked ? "동일" : rbAdminDiff.Checked ? "다름" : "";
            d.Pos.LmmAccount = rbLmmOk.Checked ? "O" : rbLmmFail.Checked ? "X" : "";
            d.Pos.PosTypes.Clear(); foreach (var kv in chkPosTypes) if (kv.Value.Checked) d.Pos.PosTypes.Add(kv.Key);
            d.Pos.TableMode = rbTablePost.Checked ? "후불" : rbTablePre.Checked ? "선불" : rbTableNone.Checked ? "비연동" : "";
            d.Network.RouterKT = txtRouterKT.Text; d.Network.RouterLG = txtRouterLG.Text;
            d.Network.RouterSK = txtRouterSK.Text; d.Network.RouterIpTime = txtRouterIpTime.Text; d.Network.RouterEtc = txtRouterEtc.Text;
            d.Network.WifiAccount = txtWifiAccount.Text; d.Network.MainPosInternalIP = txtMainPosIP.Text;
            d.Checklist.CheckExternalIP = oxExternalIP.Value; d.Checklist.CheckDHCP = oxDHCP.Value;
            d.Checklist.LocalModeMenuBoard = chkLocalMenuBoard.Checked; d.Checklist.LocalModeNoticeBoard = chkLocalNoticeBoard.Checked;
            d.Checklist.CheckFirewall = oxFirewall.Value; d.Checklist.CheckFirewallPopup = oxFirewallPopup.Value;
            d.Checklist.OrderPosCount = cmbOrderPosCount.SelectedItem?.ToString() ?? "X"; d.Checklist.OrderPosNote = txtOrderPosNote.Text;
            d.Checklist.CheckHiorderLogin = oxHiorderLogin.Value; d.Checklist.CheckSyncOrder = oxSyncOrder.Value;
            d.Checklist.PrepaidTableCheck = chkPrepaidTable.Checked; d.Checklist.PrepaidPaymentCheck = chkPrepaidPayment.Checked;
            d.Checklist.PrepaidOver5Man = chkPrepaid5Man.Checked; d.Checklist.PrepaidKSNET = chkPrepaidKSNET.Checked;
            d.Checklist.CheckTableSort = oxTableSort.Value;
            d.Checklist.WifiStatus = rbWifiGood.Checked ? "좋음" : rbWifiOk.Checked ? "양호" : rbWifiBad.Checked ? "불량" : "";
            d.Checklist.CheckMenuImage = oxMenuImage.Value; d.Checklist.CheckNoticeBoardVer = oxNoticeBoardVer.Value;
            d.Checklist.CheckNoticeBoardAdmin = oxNoticeBoardAdmin.Value; d.Checklist.CheckMenuBoardVer = oxMenuBoardVer.Value;
            d.Checklist.CheckMenuBoardAutoRun = oxMenuBoardAuto.Value; d.Checklist.CheckCoupon = oxCoupon.Value;
            d.Finish.CouponXReason = txtCouponXReason.Text; d.Finish.RemoteEduContact = txtRemoteEduContact.Text; d.Finish.InstallIssue = txtInstallIssue.Text;
        }

        public ChecklistData GetData() { if (_currentSession == null) return new ChecklistData(); Save2Ses(_currentSession); return _currentSession.Data; }

        // ═══════════════════════════════
        // 이벤트
        // ═══════════════════════════════
        private string FmtT(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim().Replace(":", "");
            if (s.Length == 1) s = "0" + s + "00";
            if (s.Length == 2) s = s + "00";
            if (s.Length == 3) s = "0" + s;
            if (s.Length == 4) { int h, m; if (int.TryParse(s.Substring(0, 2), out h) && int.TryParse(s.Substring(2, 2), out m) && h < 24 && m < 60) return string.Format("{0:D2}:{1:D2}", h, m); }
            return s;
        }

        private void UpdateElapsed()
        {
            if (_isLoading) return;
            try { TimeSpan st, et; if (!TimeSpan.TryParse(txtStartTime.Text.Trim(), out st) || !TimeSpan.TryParse(txtEndTime.Text.Trim(), out et)) return; if (et < st) et = et.Add(TimeSpan.FromHours(24)); txtElapsedTime.Text = string.Format("{0}분", (int)(et - st).TotalMinutes); } catch { }
        }

        private void UpdateStartBtn()
        {
            if (btnStart == null) return;
            bool has = !string.IsNullOrEmpty(txtStartTime.Text) || !string.IsNullOrEmpty(txtEndTime.Text);
            btnStart.Enabled = !(has && !workStartTime.HasValue);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            workStartTime = DateTime.Now; txtStartTime.Text = workStartTime.Value.ToString("HH:mm");
            btnStart.Enabled = false; btnFinish.Enabled = true; AutoSave();
        }

        private void BtnFinish_Click(object sender, EventArgs e)
        {
            var end = DateTime.Now; txtEndTime.Text = end.ToString("HH:mm");
            if (workStartTime.HasValue) txtElapsedTime.Text = string.Format("{0}분", (int)(end - workStartTime.Value).TotalMinutes);
            btnFinish.Enabled = false; AutoSave();
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            if (_currentSession == null) return;
            Save2Ses(_currentSession);
            var errs = DoValidate(_currentSession.Data);
            if (errs.Count > 0) { MessageBox.Show("필수 항목을 입력해주세요:\n\n" + string.Join("\n", errs), "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            btnRegister.Enabled = false; btnRegister.Text = "등록 중...";
            var svc = new PlaywrightService();
            var prog = new Progress<string>(msg => { if (InvokeRequired) Invoke(new Action(() => btnRegister.Text = msg)); else btnRegister.Text = msg; });
            var res = await svc.RegisterAsync(_currentSession.Data, prog);
            btnRegister.Enabled = true; btnRegister.Text = "🌐  다우오피스 자동 등록";
            if (res.Item1)
            {
                ReportService.SaveReport(_currentSession.Data);
                if (MessageBox.Show("등록 완료!\n완료 처리하시겠습니까?", "완료", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                { _workspace.CompleteSession(_currentSession.Id); ClearSession(); RefreshStoreList(); }
            }
            else MessageBox.Show("등록 실패:\n" + res.Item2, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private List<string> DoValidate(ChecklistData d)
        {
            var e = new List<string>();
            if (string.IsNullOrEmpty(d.Basic.StoreName)) e.Add("• 매장명");
            if (string.IsNullOrEmpty(d.Basic.StartTime)) e.Add("• 설치 시작시간");
            if (string.IsNullOrEmpty(d.Basic.EndTime)) e.Add("• 설치 종료시간");
            if (string.IsNullOrEmpty(d.Pos.RemoteAccount)) e.Add("• 원격 계정");
            if (string.IsNullOrEmpty(d.Pos.RemoteAdmin)) e.Add("• 원격 어드민");
            if (string.IsNullOrEmpty(d.Pos.LmmAccount)) e.Add("• LMM 계정");
            if (d.Pos.PosTypes.Count < 2) e.Add("• POS 종류 (2개 이상)");
            if (string.IsNullOrEmpty(d.Pos.TableMode)) e.Add("• 테이블 모드");
            if (string.IsNullOrEmpty(d.Checklist.CheckExternalIP)) e.Add("• 공인 IP 확인");
            if (string.IsNullOrEmpty(d.Checklist.CheckDHCP)) e.Add("• DHCP 설정");
            if (string.IsNullOrEmpty(d.Checklist.CheckFirewall)) e.Add("• 방화벽 OFF");
            if (string.IsNullOrEmpty(d.Checklist.CheckFirewallPopup)) e.Add("• 방화벽 팝업 OFF");
            if (string.IsNullOrEmpty(d.Checklist.CheckHiorderLogin)) e.Add("• 하이오더 로그인");
            if (string.IsNullOrEmpty(d.Checklist.CheckSyncOrder)) e.Add("• 동기화 & 주문 출력");
            if (string.IsNullOrEmpty(d.Checklist.CheckTableSort)) e.Add("• 테이블 정렬");
            if (string.IsNullOrEmpty(d.Checklist.WifiStatus)) e.Add("• 와이파이 상태");
            if (string.IsNullOrEmpty(d.Checklist.CheckMenuImage)) e.Add("• 메뉴 이미지 요청");
            if (string.IsNullOrEmpty(d.Checklist.CheckNoticeBoardVer)) e.Add("• 알림판 Ver");
            if (string.IsNullOrEmpty(d.Checklist.CheckNoticeBoardAdmin)) e.Add("• 알림판 어드민");
            if (string.IsNullOrEmpty(d.Checklist.CheckMenuBoardVer)) e.Add("• 메뉴판 Ver");
            if (string.IsNullOrEmpty(d.Checklist.CheckMenuBoardAutoRun)) e.Add("• 메뉴 자동실행");
            if (string.IsNullOrEmpty(d.Checklist.CheckCoupon)) e.Add("• 쿠폰 생성 여부");
            return e;
        }

        // ═══════════════════════════════
        // UI 헬퍼
        // ═══════════════════════════════
        private void AddFL(Control p, string text, int x, int y)
            => p.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = TXT_S });

        private System.Windows.Forms.TextBox AddTB(Control p, int x, int y, int w)
        {
            var tb = new System.Windows.Forms.TextBox { Location = new Point(x, y), Width = w, Height = 30, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(tb); return tb;
        }

        private RoundTextBox AddRTB(Control p, int x, int y, int w)
        {
            var tb = new RoundTextBox { Location = new Point(x, y), Width = w, Height = 42 };
            p.Controls.Add(tb); return tb;
        }

        private RoundTextBox AddRLR(Control p, string label, ref int y, int w = 220)
        {
            p.Controls.Add(new Label { Text = label, Location = new Point(0, y + 12), AutoSize = true, Font = FluentFonts.Body, ForeColor = TXT_S });
            var tb = new RoundTextBox { Location = new Point(220, y), Width = w, Height = 42 };
            p.Controls.Add(tb); y += 52; return tb;
        }

        private RadioButton AddRB(Control p, string text, int x, int y)
        {
            var rb = new RadioButton { Text = text, Location = new Point(x, y), AutoSize = true, Font = FluentFonts.Body };
            p.Controls.Add(rb); return rb;
        }

        private System.Windows.Forms.TextBox AddLR(Control p, string label, ref int y, int w = 200)
        {
            p.Controls.Add(new Label { Text = label, Location = new Point(0, y + 6), AutoSize = true, Font = FluentFonts.Body, ForeColor = TXT_S });
            var tb = new System.Windows.Forms.TextBox { Location = new Point(210, y), Width = w, Font = FluentFonts.Body, BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(tb); y += 36; return tb;
        }

        private OxRadio AddOX(Control p, string label, ref int y)
        {
            // rbO, rbX: 크기 고정, 위치 고정 (우측에서 역산)
            var rbO = new RadioButton { Text = "O", Location = new Point(999, 5), Width = 46, Height = 22, Font = FluentFonts.Body };
            var rbX = new RadioButton { Text = "X", Location = new Point(999, 5), Width = 46, Height = 22, Font = FluentFonts.Body };

            var lbl = new Label
            {
                Text = label,
                Location = new Point(0, 6),
                Height = 20,
                Font = FluentFonts.Body,
                ForeColor = TXT
            };

            var wrap = new Panel
            {
                Location = new Point(0, y),
                Height = 32,
                BackColor = Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            // wrap에 컨트롤 추가
            wrap.Controls.Add(lbl);
            wrap.Controls.Add(rbO);
            wrap.Controls.Add(rbX);
            p.Controls.Add(wrap);

            // wrap 크기 확정 후 위치 계산
            Action reposition = () =>
            {
                int w = wrap.Width > 0 ? wrap.Width : 700;
                rbX.Location = new Point(w - 48, 5);
                rbO.Location = new Point(w - 96, 5);
                lbl.Width = w - 100;
            };

            wrap.Resize += (s, e) => reposition();
            wrap.Width = p.Width > 40 ? p.Width - 40 : 700;
            p.Resize += (s, e) => { wrap.Width = p.Width > 40 ? p.Width - 40 : 700; };
            // 폼이 로드된 후 한 번 더 보정
            p.FindForm()?.Load += (s, e) => reposition();

            reposition();

            var ox = new OxRadio(label, rbO, rbX);
            y += 36;
            return ox;
        }

        private void AddDivider(Control p, int y)
            => p.Controls.Add(new Panel { Location = new Point(0, y), Size = new Size(p.Width > 20 ? p.Width - 20 : 600, 1), BackColor = BORDER });

        private void GroupRBs(params RadioButton[] rbs)
        {
            if (rbs.Length == 0 || rbs[0].Parent == null) return;
            var parent = rbs[0].Parent;
            int minX = int.MaxValue, minY = int.MaxValue, maxR = 0, maxB = 0;
            foreach (var rb in rbs) { minX = Math.Min(minX, rb.Left); minY = Math.Min(minY, rb.Top); maxR = Math.Max(maxR, rb.Right); maxB = Math.Max(maxB, rb.Bottom); }
            var wrap = new Panel { Location = new Point(minX, minY), Size = new Size(maxR - minX + 4, maxB - minY + 4), BackColor = Color.Transparent };
            foreach (var rb in rbs) { parent.Controls.Remove(rb); rb.Location = new Point(rb.Left - minX, rb.Top - minY); wrap.Controls.Add(rb); }
            parent.Controls.Add(wrap);
        }

        private void SR(RadioButton rb, bool v) { rb.Checked = v; }

        // 둥근 사각형 경로
        private static GraphicsPath RR(Rectangle r, int rad)
        {
            var p = new GraphicsPath(); int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }

    // ═══════════════════════════════
    // RoundPanel - 둥근 카드 패널
    // ═══════════════════════════════
    public class RoundPanel : Panel
    {
        private int _radius;
        public RoundPanel(int radius = 12)
        {
            _radius = radius;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = MakePath(r, _radius))
            {
                e.Graphics.FillPath(new SolidBrush(BackColor), path);
                e.Graphics.DrawPath(new Pen(Color.FromArgb(229, 229, 229), 1), path);
            }
        }

        private GraphicsPath MakePath(Rectangle r, int rad)
        {
            var p = new GraphicsPath(); int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }

    // ═══════════════════════════════
    // TaskCard - 매장 목록 카드
    // ═══════════════════════════════
    public class TaskCard : UserControl
    {
        public bool IsSelected { get; set; }
        public event Action<string> OnDelete;
        public event Action<string> OnComplete;
        public event Action<string> OnRestore;
        private StoreSession _s;
        private int _pct;
        private bool _hover = false;

        static readonly Color ACC = Color.FromArgb(0, 120, 212);
        static readonly Color SEL = Color.FromArgb(239, 246, 255);
        static readonly Color HBDR = Color.FromArgb(0, 120, 212);
        static readonly Color TM = Color.FromArgb(28, 28, 28);
        static readonly Color TS = Color.FromArgb(96, 96, 96);
        static readonly Color GRN = Color.FromArgb(16, 124, 16);
        static readonly Color YLW = Color.FromArgb(200, 130, 0);

        public TaskCard(StoreSession session, int percent)
        {
            _s = session; _pct = percent;
            Height = 86; Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

            // 삭제 버튼
            var del = new Label { Text = "✕", Font = new Font("Segoe UI", 9f), ForeColor = TS, Size = new Size(20, 20), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
            del.Location = new Point(Width - 26, 8); del.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            del.Click += (s, e) => { if (MessageBox.Show("삭제하시겠습니까?", "삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) OnDelete?.Invoke(_s.Id); };
            Controls.Add(del);

            if (session.Status != "완료")
            {
                var done = new Label { Text = "완료처리", Font = new Font("Segoe UI", 7.5f), ForeColor = ACC, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, BorderStyle = BorderStyle.FixedSingle };
                done.Location = new Point(Width - 58, 60); done.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                done.Click += (s, e) => OnComplete?.Invoke(_s.Id);
                Controls.Add(done);
            }
            else
            {
                var rest = new Label { Text = "복구", Font = new Font("Segoe UI", 7.5f), ForeColor = TS, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, BorderStyle = BorderStyle.FixedSingle };
                rest.Location = new Point(Width - 40, 60); rest.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                rest.Click += (s, e) => OnRestore?.Invoke(_s.Id);
                Controls.Add(rest);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(1, 1, Width - 2, Height - 2);
            using (var path = MkPath(r, 10))
            {
                var bg = IsSelected ? SEL : (_hover ? Color.FromArgb(248, 250, 252) : Color.White);
                e.Graphics.FillPath(new SolidBrush(bg), path);
                var pen = IsSelected ? new Pen(HBDR, 2) : new Pen(Color.FromArgb(229, 229, 229), 1);
                e.Graphics.DrawPath(pen, path);
            }
            if (IsSelected)
                e.Graphics.FillRectangle(new SolidBrush(ACC), new Rectangle(1, 12, 3, Height - 24));

            // 이니셜 원
            var initials = GetInitials(_s.DisplayName);
            var circR = new Rectangle(10, 14, 40, 40);
            e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(235, 244, 255)), circR);
            e.Graphics.DrawEllipse(new Pen(Color.FromArgb(180, 210, 240)), circR);
            TextRenderer.DrawText(e.Graphics, initials, new Font("Segoe UI", 11f, FontStyle.Bold), circR, ACC, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // 매장명
            TextRenderer.DrawText(e.Graphics, _s.DisplayName, new Font("Segoe UI", 9.5f, FontStyle.Bold), new Rectangle(58, 10, Width - 88, 22), TM, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            // 상태
            bool done = _s.Status == "완료";
            var statC = done ? GRN : YLW;
            e.Graphics.FillEllipse(new SolidBrush(statC), new Rectangle(58, 34, 8, 8));
            TextRenderer.DrawText(e.Graphics, done ? "완료" : "작성중", new Font("Segoe UI", 8.5f), new Rectangle(70, 30, 80, 16), statC, TextFormatFlags.Left);

            // 진행률 숫자
            TextRenderer.DrawText(e.Graphics, _pct + "%", new Font("Segoe UI", 8.5f, FontStyle.Bold), new Rectangle(Width - 78, 30, 48, 16), ACC, TextFormatFlags.Right);

            // 진행률 바
            var barR = new Rectangle(10, 56, Width - 20, 5);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(220, 230, 240)), barR);
            int filled = (int)(barR.Width * _pct / 100.0);
            if (filled > 0) e.Graphics.FillRectangle(new SolidBrush(ACC), new Rectangle(barR.X, barR.Y, filled, barR.Height));

            // 시간
            TextRenderer.DrawText(e.Graphics, _s.CreatedAt.ToString("MM/dd HH:mm"), new Font("Segoe UI", 8f), new Rectangle(10, 64, 120, 16), TS, TextFormatFlags.Left);
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            if (name.Length >= 2) return name.Substring(0, 2);
            return name.Substring(0, 1);
        }

        private GraphicsPath MkPath(Rectangle r, int rad)
        {
            var p = new GraphicsPath(); int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }
}