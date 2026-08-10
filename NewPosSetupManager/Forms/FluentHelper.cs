using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NewPosSetupManager.Forms
{
    // ════════════════════════════════════════
    // 색상 팔레트
    // ════════════════════════════════════════
    public static class FluentColors
    {
        public static readonly Color Background = Color.FromArgb(245, 247, 250);
        public static readonly Color Surface = Color.White;
        public static readonly Color Accent = Color.FromArgb(0, 112, 240);
        public static readonly Color AccentHover = Color.FromArgb(0, 90, 200);
        public static readonly Color AccentLight = Color.FromArgb(235, 244, 255);
        public static readonly Color NavBg = Color.FromArgb(30, 30, 40);
        public static readonly Color NavHover = Color.FromArgb(45, 45, 58);
        public static readonly Color NavSelected = Color.FromArgb(0, 112, 240);
        public static readonly Color TextPrimary = Color.FromArgb(20, 20, 30);
        public static readonly Color TextSecond = Color.FromArgb(100, 110, 130);
        public static readonly Color TextOnDark = Color.FromArgb(220, 225, 235);
        public static readonly Color Divider = Color.FromArgb(225, 228, 235);
        public static readonly Color CardBorder = Color.FromArgb(220, 224, 232);
        public static readonly Color Success = Color.FromArgb(16, 168, 100);
        public static readonly Color Warning = Color.FromArgb(240, 160, 0);
        public static readonly Color Danger = Color.FromArgb(220, 50, 50);
        public static readonly Color ProgressBg = Color.FromArgb(220, 228, 240);
    }

    // ════════════════════════════════════════
    // 폰트
    // ════════════════════════════════════════
    public static class FluentFonts
    {
        public static readonly Font Body = new Font("Segoe UI", 9.5f);
        public static readonly Font BodyBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font Caption = new Font("Segoe UI", 8.5f);
        public static readonly Font Title = new Font("Segoe UI", 13f);
        public static readonly Font TitleBold = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font NavItem = new Font("Segoe UI", 9.5f);
        public static readonly Font Small = new Font("Segoe UI", 8f);
        public static readonly Font Header = new Font("Segoe UI", 11f, FontStyle.Bold);
    }

    // ════════════════════════════════════════
    // 공통 헬퍼: 둥근 사각형 경로
    // ════════════════════════════════════════
    internal static class DrawHelper
    {
        public static GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath();
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
    // 둥근 카드 패널
    // ════════════════════════════════════════
    public class CardPanel : Panel
    {
        public int Radius { get; set; } = 10;

        public CardPanel()
        {
            this.BackColor = FluentColors.Surface;
            this.Padding = new Padding(16);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = DrawHelper.RoundedRect(r, Radius))
            {
                e.Graphics.FillPath(new SolidBrush(BackColor), path);
                e.Graphics.DrawPath(new Pen(FluentColors.CardBorder, 1), path);
            }
        }
    }

    // ════════════════════════════════════════
    // Fluent 버튼
    // ════════════════════════════════════════
    public class FluentButton : Button
    {
        public bool IsPrimary { get; set; } = false;
        public bool IsDanger { get; set; } = false;

        public FluentButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Height = 34;
            Font = FluentFonts.Body;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color bg, fg, border;
            if (IsPrimary)
            {
                bg = Enabled ? FluentColors.Accent : Color.FromArgb(180, 200, 230);
                fg = Color.White;
                border = bg;
            }
            else if (IsDanger)
            {
                bg = Color.FromArgb(255, 245, 245);
                fg = FluentColors.Danger;
                border = Color.FromArgb(240, 200, 200);
            }
            else
            {
                bg = Color.FromArgb(248, 249, 252);
                fg = FluentColors.TextPrimary;
                border = FluentColors.CardBorder;
            }

            using (var path = DrawHelper.RoundedRect(r, 6))
            {
                e.Graphics.FillPath(new SolidBrush(bg), path);
                e.Graphics.DrawPath(new Pen(border), path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ════════════════════════════════════════
    // 진행률 바
    // ════════════════════════════════════════
    public class ProgressBar2 : Control
    {
        private int _value = 0;
        private int _maximum = 100;

        public int Value
        {
            get { return _value; }
            set { _value = Math.Min(value, _maximum); Invalidate(); }
        }

        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = value; Invalidate(); }
        }

        public ProgressBar2()
        {
            this.Height = 6;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bg = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = DrawHelper.RoundedRect(bg, 3))
                e.Graphics.FillPath(new SolidBrush(FluentColors.ProgressBg), path);

            if (_maximum > 0 && _value > 0)
            {
                int w = (int)((float)_value / _maximum * Width);
                var fg = new Rectangle(0, 0, Math.Max(w - 1, Height), Height - 1);
                using (var path = DrawHelper.RoundedRect(fg, 3))
                    e.Graphics.FillPath(new SolidBrush(FluentColors.Accent), path);
            }
        }
    }

    // ════════════════════════════════════════
    // 상태 뱃지 (완료 / 진행중 / 대기)
    // ════════════════════════════════════════
    public class StatusBadge : Control
    {
        public enum BadgeStatus { Done, InProgress, Waiting }

        private BadgeStatus _status = BadgeStatus.Waiting;

        public BadgeStatus Status
        {
            get { return _status; }
            set { _status = value; Invalidate(); }
        }

        public StatusBadge()
        {
            this.Size = new Size(52, 22);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg, fg;
            string text;
            switch (_status)
            {
                case BadgeStatus.Done:
                    bg = Color.FromArgb(230, 250, 240); fg = FluentColors.Success; text = "완료"; break;
                case BadgeStatus.InProgress:
                    bg = Color.FromArgb(255, 248, 230); fg = FluentColors.Warning; text = "진행중"; break;
                default:
                    bg = Color.FromArgb(240, 242, 248); fg = FluentColors.TextSecond; text = "대기"; break;
            }
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = DrawHelper.RoundedRect(r, 10))
                e.Graphics.FillPath(new SolidBrush(bg), path);
            TextRenderer.DrawText(e.Graphics, text, FluentFonts.Small, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ════════════════════════════════════════
    // O / X 라디오 컨트롤
    // ════════════════════════════════════════
    // OxRadio: 외부 RadioButton 두 개를 래핑하는 얇은 래퍼
    public class OxRadio
    {
        private RadioButton _rbO, _rbX;
        public event Action<string> OnChanged;

        public OxRadio(string labelText, RadioButton rbO, RadioButton rbX)
        {
            _rbO = rbO; _rbX = rbX;
            _rbO.CheckedChanged += (s, e) => { if (_rbO.Checked) OnChanged?.Invoke("O"); };
            _rbX.CheckedChanged += (s, e) => { if (_rbX.Checked) OnChanged?.Invoke("X"); };
        }

        public string Value { get { return _rbO.Checked ? "O" : _rbX.Checked ? "X" : ""; } }

        public void SetValue(string v)
        {
            _rbO.Checked = (v == "O");
            _rbX.Checked = (v == "X");
        }
    }


    // ════════════════════════════════════════
    // 기존 호환 클래스 (MainForm 등에서 참조)
    // ════════════════════════════════════════
    public class NavItem : Control
    {
        public bool IsSelected { get; set; }
        public string Icon { get; set; } = "";

        public NavItem()
        {
            Height = 40;
            Font = FluentFonts.NavItem;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(4, 2, Width - 8, Height - 4);

            if (IsSelected)
            {
                using (var path = DrawHelper.RoundedRect(r, 5))
                    e.Graphics.FillPath(new SolidBrush(FluentColors.NavSelected), path);
                e.Graphics.FillRectangle(new SolidBrush(FluentColors.Accent), new Rectangle(4, 10, 3, Height - 24));
            }

            Color fg = FluentColors.TextOnDark;
            string display = string.IsNullOrEmpty(Icon) ? Text : Icon + "  " + Text;
            TextRenderer.DrawText(e.Graphics, display, Font,
                new Rectangle(r.X + 16, r.Y, r.Width - 16, r.Height), fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Invalidate(); }
    }

    public class FluentTextBox : Panel
    {
        public System.Windows.Forms.TextBox Inner { get; }

        public FluentTextBox(bool multiline = false)
        {
            BackColor = FluentColors.Surface;
            Height = multiline ? 100 : 32;
            Padding = new Padding(1);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

            Inner = new System.Windows.Forms.TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = FluentFonts.Body,
                BackColor = FluentColors.Surface,
                Multiline = multiline,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
            };
            Controls.Add(Inner);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = DrawHelper.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 5))
            using (var border = new Pen(Inner.Focused ? FluentColors.Accent : FluentColors.CardBorder, Inner.Focused ? 2 : 1))
            {
                e.Graphics.FillPath(new SolidBrush(BackColor), path);
                e.Graphics.DrawPath(border, path);
            }
        }
    }

    // ── 둥근 TextBox (Win11 스타일) ──
    public class RoundTextBox : Panel
    {
        public System.Windows.Forms.TextBox Inner { get; }
        private bool _focused = false;
        private static readonly Color BorderNormal = Color.FromArgb(217, 217, 217);
        private static readonly Color BorderFocus = Color.FromArgb(0, 120, 212);
        private const int Radius = 8;

        public new string Text
        {
            get { return Inner.Text; }
            set { Inner.Text = value; }
        }

        public bool ReadOnly
        {
            get { return Inner.ReadOnly; }
            set { Inner.ReadOnly = value; Inner.BackColor = value ? Color.FromArgb(245, 245, 245) : Color.White; }
        }

        public new Color BackColor
        {
            get { return Inner.BackColor; }
            set { Inner.BackColor = value; }
        }

        public new event EventHandler TextChanged
        {
            add { Inner.TextChanged += value; }
            remove { Inner.TextChanged -= value; }
        }

        public new event EventHandler Leave
        {
            add { Inner.Leave += value; }
            remove { Inner.Leave -= value; }
        }

        public RoundTextBox(bool multiline = false)
        {
            Height = multiline ? 100 : 42;
            base.BackColor = Color.White;
            Padding = new Padding(0);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

            Inner = new System.Windows.Forms.TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.White,
                Multiline = multiline,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
            };

            Inner.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            Inner.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            Controls.Add(Inner);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (Inner == null) return;
            int padH = 8;
            if (Inner.Multiline)
            {
                Inner.SetBounds(padH, 4, Width - padH * 2, Height - 8);
            }
            else
            {
                int padV = (Height - Inner.PreferredHeight) / 2;
                if (padV < 2) padV = 2;
                Inner.SetBounds(padH, padV, Width - padH * 2, Height - padV * 2);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = DrawHelper.RoundedRect(r, Radius))
            {
                e.Graphics.FillPath(new SolidBrush(Inner.BackColor), path);
                var penColor = _focused ? BorderFocus : BorderNormal;
                var penWidth = _focused ? 2f : 1f;
                e.Graphics.DrawPath(new Pen(penColor, penWidth), path);
            }
        }
    }

    // ── 둥근 ComboBox ──
    public class RoundComboBox : Panel
    {
        public System.Windows.Forms.ComboBox Inner { get; }
        private bool _focused = false;
        private static readonly Color BorderNormal = Color.FromArgb(217, 217, 217);
        private static readonly Color BorderFocus = Color.FromArgb(0, 120, 212);
        private const int Radius = 8;

        public object SelectedItem
        {
            get { return Inner.SelectedItem; }
            set { Inner.SelectedItem = value; }
        }

        public int SelectedIndex
        {
            get { return Inner.SelectedIndex; }
            set { Inner.SelectedIndex = value; }
        }

        public System.Windows.Forms.ComboBoxStyle DropDownStyle
        {
            get { return Inner.DropDownStyle; }
            set { Inner.DropDownStyle = value; }
        }

        public System.Windows.Forms.ComboBox.ObjectCollection Items { get { return Inner.Items; } }

        public new string Text
        {
            get { return Inner.Text; }
            set { Inner.Text = value; }
        }

        public new event EventHandler TextChanged
        {
            add { Inner.TextChanged += value; }
            remove { Inner.TextChanged -= value; }
        }

        public new event EventHandler Leave
        {
            add { Inner.Leave += value; }
            remove { Inner.Leave -= value; }
        }

        public RoundComboBox()
        {
            Height = 42;
            base.BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

            Inner = new System.Windows.Forms.ComboBox
            {
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Inner.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            Inner.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            Controls.Add(Inner);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (Inner == null) return;
            int padH = 6;
            int y = (Height - Inner.PreferredHeight) / 2;
            if (y < 2) y = 2;
            Inner.SetBounds(padH, y, Width - padH * 2, Inner.PreferredHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = DrawHelper.RoundedRect(r, Radius))
            {
                e.Graphics.FillPath(new SolidBrush(Color.White), path);
                var penColor = _focused ? BorderFocus : BorderNormal;
                var penWidth = _focused ? 2f : 1f;
                e.Graphics.DrawPath(new Pen(penColor, penWidth), path);
            }
        }
    }

    // ── 둥근 DateTimePicker ──
    public class RoundDateTimePicker : Panel
    {
        public DateTimePicker Inner { get; }
        private bool _focused = false;
        private static readonly Color BorderNormal = Color.FromArgb(217, 217, 217);
        private static readonly Color BorderFocus = Color.FromArgb(0, 120, 212);
        private const int Radius = 8;

        public DateTime Value
        {
            get { return Inner.Value; }
            set { Inner.Value = value; }
        }

        public DateTimePickerFormat Format
        {
            get { return Inner.Format; }
            set { Inner.Format = value; }
        }

        public RoundDateTimePicker()
        {
            Height = 42;
            base.BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

            Inner = new DateTimePicker
            {
                Font = new Font("Segoe UI", 9.5f),
                Format = DateTimePickerFormat.Short,
                CalendarForeColor = Color.FromArgb(28, 28, 28)
            };
            Inner.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            Inner.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            Controls.Add(Inner);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (Inner == null) return;
            int padH = 4;
            int y = (Height - Inner.PreferredHeight) / 2;
            if (y < 2) y = 2;
            Inner.SetBounds(padH, y, Width - padH * 2, Inner.PreferredHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = DrawHelper.RoundedRect(r, Radius))
            {
                e.Graphics.FillPath(new SolidBrush(Color.White), path);
                var penColor = _focused ? BorderFocus : BorderNormal;
                var penWidth = _focused ? 2f : 1f;
                e.Graphics.DrawPath(new Pen(penColor, penWidth), path);
            }
        }
    }
}
