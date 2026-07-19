using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PosSetupManager.Forms
{
    public static class FluentColors
    {
        public static readonly Color Background = Color.FromArgb(243, 243, 243);
        public static readonly Color Surface = Color.White;
        public static readonly Color Accent = Color.FromArgb(0, 103, 192);
        public static readonly Color AccentHover = Color.FromArgb(0, 84, 166);
        public static readonly Color NavBg = Color.FromArgb(238, 238, 238);
        public static readonly Color NavHover = Color.FromArgb(225, 225, 225);
        public static readonly Color NavSelected = Color.FromArgb(255, 255, 255);
        public static readonly Color TextPrimary = Color.FromArgb(28, 28, 28);
        public static readonly Color TextSecond = Color.FromArgb(96, 96, 96);
        public static readonly Color Divider = Color.FromArgb(220, 220, 220);
        public static readonly Color CardBorder = Color.FromArgb(229, 229, 229);
    }

    public static class FluentFonts
    {
        public static readonly Font Body = new Font("Segoe UI", 9.5f);
        public static readonly Font BodyBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font Caption = new Font("Segoe UI", 8.5f);
        public static readonly Font Title = new Font("Segoe UI", 13f, FontStyle.Regular);
        public static readonly Font NavItem = new Font("Segoe UI", 9.5f);
    }

    // 둥근 모서리 패널 (카드)
    public class CardPanel : Panel
    {
        public int Radius { get; set; } = 8;

        public CardPanel()
        {
            this.BackColor = FluentColors.Surface;
            this.Padding = new Padding(16);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedRect(ClientRectangle, Radius))
            using (var border = new Pen(FluentColors.CardBorder, 1))
            {
                e.Graphics.FillPath(new SolidBrush(BackColor), path);
                e.Graphics.DrawPath(border, path);
            }
        }

        private GraphicsPath RoundedRect(Rectangle r, int rad)
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

    // Fluent 스타일 버튼
    public class FluentButton : Button
    {
        public bool IsPrimary { get; set; } = false;

        public FluentButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 1;
            Height = 32;
            Font = FluentFonts.Body;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = ClientRectangle;
            Color bg = IsPrimary ? FluentColors.Accent : Color.FromArgb(249, 249, 249);
            Color border = IsPrimary ? FluentColors.Accent : FluentColors.CardBorder;
            Color fg = IsPrimary ? Color.White : FluentColors.TextPrimary;

            using (var path = RoundedRect(new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1), 5))
            {
                e.Graphics.FillPath(new SolidBrush(bg), path);
                e.Graphics.DrawPath(new Pen(border), path);
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath RoundedRect(Rectangle r, int rad)
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

    // 좌측 네비게이션 아이템
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
                using (var path = RoundedRect(r, 5))
                    e.Graphics.FillPath(new SolidBrush(FluentColors.NavSelected), path);
                // 액센트 바
                using (var brush = new SolidBrush(FluentColors.Accent))
                    e.Graphics.FillRectangle(brush, new Rectangle(4, 10, 3, Height - 24));
            }

            Color fg = IsSelected ? FluentColors.TextPrimary : FluentColors.TextSecond;
            string display = string.IsNullOrEmpty(Icon) ? Text : $"{Icon}  {Text}";
            TextRenderer.DrawText(e.Graphics, display, Font,
                new Rectangle(r.X + 16, r.Y, r.Width - 16, r.Height), fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Invalidate();
        }

        private GraphicsPath RoundedRect(Rectangle r, int rad)
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

    // Fluent 스타일 TextBox 래퍼
    public class FluentTextBox : Panel
    {
        public TextBox Inner { get; }

        public FluentTextBox(bool multiline = false)
        {
            BackColor = FluentColors.Surface;
            Height = multiline ? 100 : 32;
            Padding = new Padding(1);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

            Inner = new TextBox
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
            using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 5))
            using (var border = new Pen(Inner.Focused ? FluentColors.Accent : FluentColors.CardBorder, Inner.Focused ? 2 : 1))
            {
                e.Graphics.FillPath(new SolidBrush(BackColor), path);
                e.Graphics.DrawPath(border, path);
            }
        }

        private GraphicsPath RoundedRect(Rectangle r, int rad)
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
}