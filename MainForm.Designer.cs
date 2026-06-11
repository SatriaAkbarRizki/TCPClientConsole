using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

#nullable enable

public partial class MainForm
{
    // ══════════════════════════════════════════════════
    //  Light Theme — Color Palette
    // ══════════════════════════════════════════════════

    private static readonly Color BgMain     = Color.FromArgb(245, 247, 251);
    private static readonly Color BgCard     = Color.FromArgb(255, 255, 255);
    private static readonly Color BgInput    = Color.FromArgb(241, 243, 248);
    private static readonly Color BdrLight   = Color.FromArgb(215, 218, 228);
    private static readonly Color BdrFocus   = Color.FromArgb(99, 102, 241);
    private static readonly Color Accent     = Color.FromArgb(79, 70, 229);
    private static readonly Color AccentHvr  = Color.FromArgb(99, 102, 241);
    private static readonly Color AccentGrd  = Color.FromArgb(139, 92, 246);
    private static readonly Color TxtPri     = Color.FromArgb(17, 24, 39);
    private static readonly Color TxtSec     = Color.FromArgb(107, 114, 128);
    private static readonly Color TxtOnAcc   = Color.FromArgb(255, 255, 255);
    private static readonly Color TxtErr     = Color.FromArgb(220, 38, 38);
    private static readonly Color BubSelf    = Color.FromArgb(79, 70, 229);
    private static readonly Color BubOther   = Color.FromArgb(241, 243, 248);
    private static readonly Color HdrBg      = Color.FromArgb(255, 255, 255);
    private static readonly Color HdrLine    = Color.FromArgb(229, 231, 235);

    // ══════════════════════════════════════════════════
    //  Inner Types
    // ══════════════════════════════════════════════════

    /// <summary>Panel dengan DoubleBuffered aktif agar tidak flicker saat custom paint.</summary>
    private class DP : Panel
    {
        public DP()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }

    // ══════════════════════════════════════════════════
    //  Layout Constants & Shared Font
    // ══════════════════════════════════════════════════

    private const int FW = 520, FH = 640, TBH = 40;
    private static readonly Font ChatFont = new Font("Segoe UI", 9.5f);

    // ══════════════════════════════════════════════════
    //  UI Control Fields
    // ══════════════════════════════════════════════════

    // Panels
    private DP panelLogin = null!;
    private DP panelChat = null!;
    private Label lblTitleText = null!;

    // Login
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private DP emailWrap = null!, passWrap = null!;
    private DP btnLogin = null!;
    private Label lblLoginBtn = null!;
    private Label lblStatus = null!;
    
    // Sliding login/config wrappers
    private DP panelLoginFields = null!;
    private DP panelConfigFields = null!;

    // Config controls
    private TextBox txtServerIp = null!;
    private TextBox txtServerPort = null!;
    private DP ipWrap = null!, portWrap = null!;
    private DP btnSaveConfig = null!;
    private Label lblSaveConfigBtn = null!;
    private Label lblGoConfig = null!;
    private Label lblBackLogin = null!;

    // Animation timer for config slide
    private System.Windows.Forms.Timer configAnimTimer = null!;
    private float configAnimProgress;
    private bool configShowTarget;

    // Chat
    private Panel chatScrollPanel = null!;
    private FlowLayoutPanel chatFlow = null!;
    private TextBox txtInput = null!;
    private DP inputWrap = null!;
    private DP btnSend = null!;
    private Label lblSendBtn = null!;
    private Label lblWelcome = null!;

    // Animasi & Drag
    private System.Windows.Forms.Timer animTimer = null!;
    private float animProgress;
    private bool isDragging;
    private Point dragStart;

    // ══════════════════════════════════════════════════
    //  InitializeComponent — Membangun seluruh UI
    // ══════════════════════════════════════════════════

    private void InitializeComponent()
    {
        FormBorderStyle = FormBorderStyle.None;
        Size = new Size(FW, FH);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgMain;
        DoubleBuffered = true;

        // Border halus di tepi form (agar terlihat di desktop putih)
        Paint += (s, e) =>
        {
            using var pen = new Pen(HdrLine, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        BuildTitleBar();
        BuildLoginPanel();
        BuildChatPanel();

        animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        animTimer.Tick += AnimTick;

        configAnimTimer = new System.Windows.Forms.Timer { Interval = 16 };
        configAnimTimer.Tick += ConfigAnimTick;
    }

    // ══════════════════════════════════════════════════
    //  Title Bar
    // ══════════════════════════════════════════════════

    private void BuildTitleBar()
    {
        var bar = new DP { Dock = DockStyle.Top, Height = TBH, BackColor = HdrBg };
        bar.Paint += (s, e) =>
        {
            using var pen = new Pen(HdrLine, 1);
            e.Graphics.DrawLine(pen, 0, TBH - 1, FW, TBH - 1);
        };

        lblTitleText = new Label
        {
            Text = "  💬  TCP Chat Client",
            ForeColor = TxtPri,
            Font = new Font("Segoe UI Semibold", 10f),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var btnClose = MakeTitleBtn("✕", true);
        btnClose.Click += (s, e) => Close();

        var btnMin = MakeTitleBtn("─", false);
        btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

        // Urutan Windows: [─ Minimize] [✕ Close]  ← Close paling kanan
        // Dengan DockStyle.Right, kontrol yang ditambahkan TERAKHIR menempel paling kanan (luar).
        bar.Controls.Add(lblTitleText);
        bar.Controls.Add(btnMin);
        bar.Controls.Add(btnClose);

        // Drag untuk memindahkan window
        lblTitleText.MouseDown += (s, e) => { isDragging = true; dragStart = e.Location; };
        lblTitleText.MouseMove += (s, e) =>
        {
            if (isDragging)
                Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
        };
        lblTitleText.MouseUp += (s, e) => isDragging = false;

        Controls.Add(bar);
    }

    private Label MakeTitleBtn(string text, bool isClose)
    {
        var lbl = new Label
        {
            Text = text,
            Size = new Size(46, TBH),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TxtSec,
            Font = new Font("Segoe UI", 9f),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        lbl.MouseEnter += (s, e) =>
        {
            lbl.BackColor = isClose ? Color.FromArgb(220, 38, 38) : Color.FromArgb(243, 244, 246);
            lbl.ForeColor = isClose ? TxtOnAcc : TxtPri;
        };
        lbl.MouseLeave += (s, e) =>
        {
            lbl.BackColor = Color.Transparent;
            lbl.ForeColor = TxtSec;
        };
        return lbl;
    }

    // ══════════════════════════════════════════════════
    //  Login Panel
    // ══════════════════════════════════════════════════

    private void BuildLoginPanel()
    {
        int contentH = FH - TBH;
        panelLogin = new DP
        {
            Location = new Point(0, TBH),
            Size = new Size(FW, contentH),
            BackColor = BgMain
        };
        panelLogin.Paint += PaintLoginBg;

        int width = FW; // 520

        // Judul
        var lblTitle = new Label
        {
            Text = "💬 TCP Chat",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = TxtPri,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 60),
            Size = new Size(width, 45),
            BackColor = Color.Transparent
        };

        var lblSub = new Label
        {
            Text = "Masuk ke ruang chat Anda",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TxtSec,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 110),
            Size = new Size(width, 22),
            BackColor = Color.Transparent
        };

        // ── cardInner (Container Slide) ──
        var cardInner = new DP
        {
            Location = new Point(0, 150),
            Size = new Size(width, 280),
            BackColor = Color.Transparent
        };

        // ── SLIDE 1: LOGIN FIELDS ──
        panelLoginFields = new DP
        {
            Location = new Point(0, 0),
            Size = new Size(width, 280),
            BackColor = Color.Transparent
        };

        int inputW = 380;
        int inputX = (width - inputW) / 2; // (520 - 380) / 2 = 70

        // Email
        var lblEmail = new Label
        {
            Text = "Email",
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = TxtPri,
            Location = new Point(inputX, 10),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        emailWrap = CreateInputWrap(inputX, 33, inputW, out txtEmail, false);

        // Password
        var lblPass = new Label
        {
            Text = "Password",
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = TxtPri,
            Location = new Point(inputX, 95),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        passWrap = CreateInputWrap(inputX, 118, inputW, out txtPassword, true);

        // Tombol login
        btnLogin = new DP
        {
            Location = new Point(inputX, 185),
            Size = new Size(inputW, 46),
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        lblLoginBtn = new Label
        {
            Text = "Masuk",
            ForeColor = TxtOnAcc,
            Font = new Font("Segoe UI Semibold", 11.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };

        bool loginHover = false;
        btnLogin.Paint += (s, e) => PaintButton(e.Graphics, btnLogin.Size, loginHover, true);
        btnLogin.MouseEnter += (s, e) => { loginHover = true; btnLogin.Invalidate(); };
        btnLogin.MouseLeave += (s, e) => { loginHover = false; btnLogin.Invalidate(); };
        lblLoginBtn.MouseEnter += (s, e) => { loginHover = true; btnLogin.Invalidate(); };
        lblLoginBtn.MouseLeave += (s, e) => { loginHover = false; btnLogin.Invalidate(); };
        btnLogin.Controls.Add(lblLoginBtn);
        btnLogin.Click += BtnLogin_Click;
        lblLoginBtn.Click += BtnLogin_Click;

        txtPassword.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BtnLogin_Click(s, e); }
        };

        // Link Pengaturan Koneksi
        lblGoConfig = new Label
        {
            Text = "Pengaturan Koneksi",
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = TxtSec,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(inputX, 245),
            Size = new Size(inputW, 25),
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        lblGoConfig.MouseEnter += (s, e) => { lblGoConfig.ForeColor = Accent; lblGoConfig.Font = new Font(lblGoConfig.Font, FontStyle.Underline); };
        lblGoConfig.MouseLeave += (s, e) => { lblGoConfig.ForeColor = TxtSec; lblGoConfig.Font = new Font(lblGoConfig.Font, FontStyle.Regular); };
        lblGoConfig.Click += BtnGoConfig_Click;

        panelLoginFields.Controls.AddRange(new Control[] {
            lblEmail, emailWrap, lblPass, passWrap, btnLogin, lblGoConfig
        });


        // ── SLIDE 2: CONFIG FIELDS ──
        panelConfigFields = new DP
        {
            Location = new Point(width, 0), // mulai di luar area (kanan)
            Size = new Size(width, 280),
            BackColor = Color.Transparent
        };

        // IP Server
        var lblIp = new Label
        {
            Text = "IP Server",
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = TxtPri,
            Location = new Point(inputX, 10),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        ipWrap = CreateInputWrap(inputX, 33, inputW, out txtServerIp, false);
        txtServerIp.Text = "127.0.0.1"; // Default IP

        // Port Server
        var lblPort = new Label
        {
            Text = "Port Server",
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = TxtPri,
            Location = new Point(inputX, 95),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        portWrap = CreateInputWrap(inputX, 118, inputW, out txtServerPort, false);
        txtServerPort.Text = "8888"; // Default Port

        // Tombol Simpan
        btnSaveConfig = new DP
        {
            Location = new Point(inputX, 185),
            Size = new Size(inputW, 46),
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        lblSaveConfigBtn = new Label
        {
            Text = "Simpan",
            ForeColor = TxtOnAcc,
            Font = new Font("Segoe UI Semibold", 11.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };

        bool saveHover = false;
        btnSaveConfig.Paint += (s, e) => PaintButton(e.Graphics, btnSaveConfig.Size, saveHover, true);
        btnSaveConfig.MouseEnter += (s, e) => { saveHover = true; btnSaveConfig.Invalidate(); };
        btnSaveConfig.MouseLeave += (s, e) => { saveHover = false; btnSaveConfig.Invalidate(); };
        lblSaveConfigBtn.MouseEnter += (s, e) => { saveHover = true; btnSaveConfig.Invalidate(); };
        lblSaveConfigBtn.MouseLeave += (s, e) => { saveHover = false; btnSaveConfig.Invalidate(); };
        btnSaveConfig.Controls.Add(lblSaveConfigBtn);
        btnSaveConfig.Click += BtnSaveConfig_Click;
        lblSaveConfigBtn.Click += BtnSaveConfig_Click;

        // Link Kembali
        lblBackLogin = new Label
        {
            Text = "Kembali ke Login",
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = TxtSec,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(inputX, 245),
            Size = new Size(inputW, 25),
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        lblBackLogin.MouseEnter += (s, e) => { lblBackLogin.ForeColor = Accent; lblBackLogin.Font = new Font(lblBackLogin.Font, FontStyle.Underline); };
        lblBackLogin.MouseLeave += (s, e) => { lblBackLogin.ForeColor = TxtSec; lblBackLogin.Font = new Font(lblBackLogin.Font, FontStyle.Regular); };
        lblBackLogin.Click += BtnBackLogin_Click;

        panelConfigFields.Controls.AddRange(new Control[] {
            lblIp, ipWrap, lblPort, portWrap, btnSaveConfig, lblBackLogin
        });


        // Add slides to inner card
        cardInner.Controls.AddRange(new Control[] { panelLoginFields, panelConfigFields });

        // Status / error (di luar slide, tetap di bawah card)
        lblStatus = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = TxtErr,
            Location = new Point(inputX, 440),
            Size = new Size(inputW, 40),
            TextAlign = ContentAlignment.TopCenter,
            BackColor = Color.Transparent
        };

        panelLogin.Controls.AddRange(new Control[] {
            lblTitle, lblSub, cardInner, lblStatus
        });

        Controls.Add(panelLogin);
    }

    // ══════════════════════════════════════════════════
    //  Chat Panel
    // ══════════════════════════════════════════════════

    private void BuildChatPanel()
    {
        int contentH = FH - TBH;
        panelChat = new DP
        {
            Location = new Point(FW, TBH), // mulai di luar layar (kanan)
            Size = new Size(FW, contentH),
            BackColor = BgMain
        };

        // Header
        int headerH = 62;
        var header = new DP
        {
            Location = new Point(0, 0),
            Size = new Size(FW, headerH),
            BackColor = HdrBg
        };
        header.Paint += (s, e) =>
        {
            using var pen = new Pen(HdrLine, 1);
            e.Graphics.DrawLine(pen, 0, headerH - 1, FW, headerH - 1);
        };

        lblWelcome = new Label
        {
            Text = "Chat Room",
            Font = new Font("Segoe UI Semibold", 13f),
            ForeColor = TxtPri,
            Location = new Point(20, 8),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var lblOnline = new Label
        {
            Text = "● Online",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(34, 197, 94),
            Location = new Point(22, 35),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        header.Controls.AddRange(new Control[] { lblWelcome, lblOnline });

        // Chat area
        int inputBarH = 65;
        int chatH = contentH - headerH - inputBarH;

        chatScrollPanel = new DP
        {
            Location = new Point(0, headerH),
            Size = new Size(FW, chatH),
            AutoScroll = true,
            BackColor = BgMain
        };

        chatFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Width = FW - 20,
            BackColor = BgMain,
            Padding = new Padding(5, 8, 5, 8)
        };
        chatScrollPanel.Controls.Add(chatFlow);

        // Input bar
        var inputBar = new DP
        {
            Location = new Point(0, headerH + chatH),
            Size = new Size(FW, inputBarH),
            BackColor = HdrBg
        };
        inputBar.Paint += (s, e) =>
        {
            using var pen = new Pen(HdrLine, 1);
            e.Graphics.DrawLine(pen, 0, 0, FW, 0);
        };

        int padX = 15, padY = 12;
        int sendW = 80;
        int inputW = FW - padX * 2 - sendW - 10;

        inputWrap = CreateInputWrap(padX, padY, inputW, out txtInput, false);

        txtInput.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BtnKirim_Click(s, e); }
        };

        // Tombol kirim
        btnSend = new DP
        {
            Location = new Point(padX + inputW + 10, padY),
            Size = new Size(sendW, 42),
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        lblSendBtn = new Label
        {
            Text = "Kirim",
            ForeColor = TxtOnAcc,
            Font = new Font("Segoe UI Semibold", 10f),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };

        bool sendHover = false;
        btnSend.Paint += (s, e) => PaintButton(e.Graphics, btnSend.Size, sendHover, false);
        btnSend.MouseEnter += (s, e) => { sendHover = true; btnSend.Invalidate(); };
        btnSend.MouseLeave += (s, e) => { sendHover = false; btnSend.Invalidate(); };
        lblSendBtn.MouseEnter += (s, e) => { sendHover = true; btnSend.Invalidate(); };
        lblSendBtn.MouseLeave += (s, e) => { sendHover = false; btnSend.Invalidate(); };

        btnSend.Controls.Add(lblSendBtn);
        btnSend.Click += BtnKirim_Click;
        lblSendBtn.Click += BtnKirim_Click;

        inputBar.Controls.AddRange(new Control[] { inputWrap, btnSend });
        panelChat.Controls.AddRange(new Control[] { header, chatScrollPanel, inputBar });
        Controls.Add(panelChat);
    }

    // ══════════════════════════════════════════════════
    //  Helper: Rounded Input Field
    // ══════════════════════════════════════════════════

    private DP CreateInputWrap(int x, int y, int w, out TextBox textBox, bool isPassword)
    {
        var wrap = new DP
        {
            Location = new Point(x, y),
            Size = new Size(w, 42),
            BackColor = BgInput,
            Cursor = Cursors.IBeam
        };

        textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = BgInput,
            ForeColor = TxtPri,
            Font = new Font("Segoe UI", 11f),
            Location = new Point(14, 11),
            Size = new Size(w - 28, 20)
        };
        if (isPassword) textBox.UseSystemPasswordChar = true;

        var tb = textBox;
        wrap.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, wrap.Width - 1, wrap.Height - 1);
            bool focused = tb.Focused;

            using (var path = RoundedRect(rect, 10))
            {
                using (var brush = new SolidBrush(BgInput))
                    g.FillPath(brush, path);
                using (var pen = new Pen(focused ? BdrFocus : BdrLight, focused ? 1.5f : 1f))
                    g.DrawPath(pen, path);
            }

            if (focused)
            {
                var glowRect = Rectangle.Inflate(rect, 3, 3);
                using (var glowPath = RoundedRect(glowRect, 13))
                using (var glowPen = new Pen(Color.FromArgb(30, BdrFocus), 4f))
                    g.DrawPath(glowPen, glowPath);
            }
        };

        tb.GotFocus += (s, e) => wrap.Invalidate();
        tb.LostFocus += (s, e) => wrap.Invalidate();
        wrap.Click += (s, e) => tb.Focus();
        wrap.Controls.Add(textBox);
        return wrap;
    }

    // ══════════════════════════════════════════════════
    //  Paint: Login Background Glow (halus)
    // ══════════════════════════════════════════════════

    private void PaintLoginBg(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;

        // Soft indigo glow (kanan atas)
        using (var ep = new GraphicsPath())
        {
            ep.AddEllipse(FW - 220, -80, 360, 360);
            using var brush = new PathGradientBrush(ep);
            brush.CenterColor = Color.FromArgb(20, 99, 102, 241);
            brush.SurroundColors = new[] { Color.Transparent };
            g.FillPath(brush, ep);
        }

        // Soft violet glow (kiri bawah)
        using (var ep = new GraphicsPath())
        {
            ep.AddEllipse(-90, panelLogin.Height - 220, 340, 340);
            using var brush = new PathGradientBrush(ep);
            brush.CenterColor = Color.FromArgb(16, 139, 92, 246);
            brush.SurroundColors = new[] { Color.Transparent };
            g.FillPath(brush, ep);
        }
    }

    // ══════════════════════════════════════════════════
    //  Paint: Login Card (white + shadow)
    // ══════════════════════════════════════════════════

    private void PaintCard(object? sender, PaintEventArgs e)
    {
        var card = (Control)sender!;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);

        // Shadow layers (terlihat jelas di background terang)
        for (int i = 0; i < 4; i++)
        {
            var sr = new Rectangle(1 + i, 2 + i * 2, rect.Width, rect.Height);
            using var sp = RoundedRect(sr, 18 + i);
            using var sb = new SolidBrush(Color.FromArgb(10 - i * 2, 0, 0, 40));
            g.FillPath(sb, sp);
        }

        // Card putih
        using (var path = RoundedRect(rect, 16))
        {
            using (var brush = new SolidBrush(BgCard))
                g.FillPath(brush, path);
            using (var pen = new Pen(HdrLine, 1f))
                g.DrawPath(pen, path);
        }
    }

    // ══════════════════════════════════════════════════
    //  Paint: Rounded Button (gradient)
    // ══════════════════════════════════════════════════

    private void PaintButton(Graphics g, Size size, bool hovered, bool useGradient)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, size.Width - 1, size.Height - 1);

        // Shadow
        var sr = new Rectangle(2, 3, rect.Width, rect.Height);
        using (var sp = RoundedRect(sr, 14))
        using (var sb = new SolidBrush(Color.FromArgb(25, 79, 70, 229)))
            g.FillPath(sb, sp);

        // Body
        using (var path = RoundedRect(rect, 12))
        {
            if (useGradient)
            {
                var c1 = hovered ? AccentHvr : Accent;
                var c2 = hovered ? Color.FromArgb(120, 80, 255) : AccentGrd;
                using (var brush = new LinearGradientBrush(rect, c1, c2, 45f))
                    g.FillPath(brush, path);
            }
            else
            {
                using (var brush = new SolidBrush(hovered ? AccentHvr : Accent))
                    g.FillPath(brush, path);
            }
        }
    }

    // ══════════════════════════════════════════════════
    //  Chat Bubble — Membuat satu baris pesan chat
    // ══════════════════════════════════════════════════

    private static Color GetAvatarColor(char c)
    {
        Color[] colors = new Color[]
        {
            Color.FromArgb(99, 102, 241),  // Indigo
            Color.FromArgb(139, 92, 246),  // Violet
            Color.FromArgb(59, 130, 246),  // Blue
            Color.FromArgb(16, 185, 129),  // Emerald
            Color.FromArgb(245, 158, 11),  // Amber
            Color.FromArgb(239, 68, 68),   // Red
            Color.FromArgb(236, 72, 153),  // Pink
            Color.FromArgb(20, 184, 166)   // Teal
        };
        int index = Math.Abs(c) % colors.Length;
        return colors[index];
    }

    internal void CreateBubbleRow(string text, bool isSelf, bool isSystem)
    {
        string senderName = "";
        string messageText = text;

        if (!isSystem && text.StartsWith("[") && text.Contains("]: "))
        {
            int idx = text.IndexOf("]: ");
            senderName = text.Substring(1, idx - 1);
            messageText = text.Substring(idx + 3);
        }

        int maxBubbleW;
        if (!isSelf && !isSystem && !string.IsNullOrEmpty(senderName))
        {
            maxBubbleW = (int)((FW - 98) * 0.72);
        }
        else
        {
            maxBubbleW = (int)((FW - 40) * 0.72);
        }

        var textSize = TextRenderer.MeasureText(messageText, ChatFont,
            new Size(maxBubbleW - 28, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        int bubbleW = Math.Min(textSize.Width + 30, maxBubbleW);
        int bubbleH = textSize.Height + 18;
        int rowW = chatFlow.Width - chatFlow.Padding.Horizontal;

        // Container baris (full width untuk alignment)
        var row = new Panel
        {
            Width = rowW,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 3, 0, 3)
        };

        int bubbleX;
        int bubbleY;

        if (isSystem)
        {
            bubbleX = (rowW - bubbleW) / 2;
            bubbleY = 4;
            row.Height = bubbleH + 8;
        }
        else if (isSelf)
        {
            bubbleX = rowW - bubbleW - 12;
            bubbleY = 4;
            row.Height = bubbleH + 8;
        }
        else // Other client
        {
            bubbleX = 58;
            bubbleY = 22;
            row.Height = Math.Max(bubbleH + 22 + 8, 48);

            // Add avatar
            string firstLetter = senderName.Length > 0 ? senderName.Substring(0, 1).ToUpper() : "?";
            Color avatarBg = GetAvatarColor(firstLetter[0]);

            var avatar = new DP
            {
                Size = new Size(36, 36),
                Location = new Point(12, 4),
                BackColor = Color.Transparent
            };
            avatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                
                using (var brush = new SolidBrush(avatarBg))
                {
                    g.FillEllipse(brush, 0, 0, 35, 35);
                }
                
                using (var font = new Font("Segoe UI", 10f, FontStyle.Bold))
                {
                    var size = g.MeasureString(firstLetter, font);
                    float lx = (36 - size.Width) / 2f;
                    float ly = (36 - size.Height) / 2f;
                    g.DrawString(firstLetter, font, Brushes.White, lx, ly);
                }
            };
            row.Controls.Add(avatar);

            // Add name label
            var lblName = new Label
            {
                Text = senderName,
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TxtSec,
                Location = new Point(58, 4),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            row.Controls.Add(lblName);
        }

        var bgColor = isSystem ? Color.FromArgb(235, 237, 242) : (isSelf ? BubSelf : BubOther);
        var txtColor = isSystem ? TxtSec : (isSelf ? TxtOnAcc : TxtPri);

        var bubble = new DP
        {
            Location = new Point(bubbleX, bubbleY),
            Size = new Size(bubbleW, bubbleH),
            BackColor = Color.Transparent
        };

        bubble.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var rect = new Rectangle(0, 0, bubble.Width - 1, bubble.Height - 1);

            // Shadow
            if (!isSystem)
            {
                var sr = new Rectangle(1, 2, rect.Width, rect.Height);
                using (var sp = RoundedRect(sr, 14))
                using (var sb = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                    g.FillPath(sb, sp);
            }

            // Bubble
            using (var path = RoundedRect(rect, 14))
            using (var brush = new SolidBrush(bgColor))
                g.FillPath(brush, path);

            // Text
            TextRenderer.DrawText(g, messageText, ChatFont,
                new Rectangle(14, 9, bubbleW - 28, bubbleH),
                txtColor,
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        };

        row.Controls.Add(bubble);
        chatFlow.Controls.Add(row);

        // ── Animasi slide-up ──
        int targetY = bubble.Top;
        bubble.Top = targetY + 16;

        var anim = new System.Windows.Forms.Timer { Interval = 16 };
        float progress = 0;
        anim.Tick += (s, e2) =>
        {
            progress += 0.12f;
            if (progress >= 1f)
            {
                progress = 1f;
                anim.Stop();
                anim.Dispose();
            }
            float ease = 1f - (float)Math.Pow(1 - progress, 3);
            bubble.Top = targetY + (int)(16 * (1 - ease));
        };
        anim.Start();

        chatScrollPanel.ScrollControlIntoView(row);
    }

    // ══════════════════════════════════════════════════
    //  Helper: GraphicsPath — Rounded Rectangle
    // ══════════════════════════════════════════════════

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
