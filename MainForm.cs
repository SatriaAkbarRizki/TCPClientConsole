using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// Logika form: menghubungkan UI (Designer) dengan TcpClientService (TCP).
/// Berisi event handler, animasi, dan thread-safe UI update.
/// </summary>
public partial class MainForm : Form
{
    // ══════════════════════════════════════════════════════════════
    //  P/Invoke — Drop Shadow & DWM Rounded Corners
    // ══════════════════════════════════════════════════════════════

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int val, int size);

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  State
    // ══════════════════════════════════════════════════════════════

    private readonly TcpClientService _tcpService = new TcpClientService();

    // ══════════════════════════════════════════════════════════════
    //  Constructor
    // ══════════════════════════════════════════════════════════════

    public MainForm()
    {
        InitializeComponent();

        HandleCreated += (s, e) =>
        {
            try
            {
                int round = 2; // DWMWCP_ROUND (Windows 11)
                DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int));
            }
            catch { }
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  Login — Menjembatani UI dengan TcpClientService
    // ══════════════════════════════════════════════════════════════

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPassword.Text))
        {
            ShowStatus("Ensure not have empty", isError: true);
            return;
        }

        ShowStatus("Menghubungkan ke server...", isError: false);
        Application.DoEvents();

        var (success, nameClient, historyJson, errorMsg) =
            _tcpService.Login(txtEmail.Text, txtPassword.Text);

        if (!success)
        {
            ShowStatus(errorMsg, isError: true);
            return;
        }

        lblWelcome.Text = $"Selamat Datang, {nameClient}!";
        lblTitleText.Text = $"  💬  TCP Chat — {nameClient}";

    
        LoadHistoryToUI(historyJson);

   
        _tcpService.MessageReceived += OnMessageReceived;
        _tcpService.Disconnected    += OnDisconnected;

        _tcpService.StartListening();

    
        AnimateToChat();
    }



    private void BtnKirim_Click(object? sender, EventArgs e)
    {
        string pesan = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(pesan)) return;

        _tcpService.SendMessage(pesan);
        txtInput.Text = "";
    }



    private void OnMessageReceived(string pesanMasuk)
    {
        this.Invoke((MethodInvoker)delegate
        {
            bool isSelf = pesanMasuk.StartsWith($"[{_tcpService.NameClient}]");
            AddChatBubble(pesanMasuk, isSelf);
        });
    }

    private void OnDisconnected()
    {
        try
        {
            this.Invoke((MethodInvoker)delegate
            {
                AddSystemMessage("[SISTEM] Koneksi ke server terputus.");
            });
        }
        catch { }
    }



    private void LoadHistoryToUI(string historyJson)
    {
        var riwayat = _tcpService.ParseHistory(historyJson);
        if (riwayat.Count == 0) return;

        AddSystemMessage("— Riwayat Chat —");
        foreach (var chat in riwayat)
        {
            bool isSelf = chat.SenderName == _tcpService.NameClient;
            AddChatBubble($"[{chat.SenderName}]: {chat.MessageText}", isSelf);
        }
        AddSystemMessage("— — —");
    }

    private void AddSystemMessage(string text) =>
        CreateBubbleRow(text, isSelf: false, isSystem: true);

    private void AddChatBubble(string text, bool isSelf) =>
        CreateBubbleRow(text, isSelf, isSystem: false);


    private void ShowStatus(string message, bool isError)
    {
        lblStatus.ForeColor = isError ? TxtErr : TxtSec;
        lblStatus.Text = message;
    }



    private void AnimateToChat()
    {
        animProgress = 0f;
        animTimer.Start();
    }

    private void AnimTick(object? sender, EventArgs e)
    {
        animProgress += 0.04f;

        if (animProgress >= 1f)
        {
            animProgress = 1f;
            animTimer.Stop();
            panelLogin.Visible = false;
            txtInput.Focus();
        }

        float t = EaseOutCubic(animProgress);
        panelLogin.Left = (int)(-FW * t);
        panelChat.Left  = (int)(FW * (1f - t));
    }

    private static float EaseOutCubic(float t) =>
        1f - (float)Math.Pow(1 - t, 3);

    // ══════════════════════════════════════════════════════════════
    //  Connection Config Handlers
    // ══════════════════════════════════════════════════════════════

    private void BtnGoConfig_Click(object? sender, EventArgs e)
    {
        ShowStatus("", false);
        configShowTarget = true;
        configAnimProgress = 0f;
        configAnimTimer.Start();
    }

    private void BtnBackLogin_Click(object? sender, EventArgs e)
    {
        ShowStatus("", false);
        configShowTarget = false;
        configAnimProgress = 0f;
        configAnimTimer.Start();
    }

    private void BtnSaveConfig_Click(object? sender, EventArgs e)
    {
        string ip = txtServerIp.Text.Trim();
        string portText = txtServerPort.Text.Trim();

        if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(portText))
        {
            ShowStatus("IP dan Port tidak boleh kosong.", isError: true);
            return;
        }

        if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
        {
            ShowStatus("Port harus berupa angka 1 - 65535.", isError: true);
            return;
        }

        _tcpService.changeConfigServer(ip, port);
        ShowStatus($"Konfigurasi disimpan: {ip}:{port}", isError: false);

        configShowTarget = false;
        configAnimProgress = 0f;
        configAnimTimer.Start();
    }

    private void ConfigAnimTick(object? sender, EventArgs e)
    {
        configAnimProgress += 0.08f;

        if (configAnimProgress >= 1f)
        {
            configAnimProgress = 1f;
            configAnimTimer.Stop();
        }

        float t = EaseOutCubic(configAnimProgress);
        int cw = 380; // Card width

        if (configShowTarget) // Ke Config
        {
            panelLoginFields.Left = (int)(-cw * t);
            panelConfigFields.Left = (int)(cw * (1f - t));
        }
        else // Ke Login
        {
            panelLoginFields.Left = (int)(-cw * (1f - t));
            panelConfigFields.Left = (int)(cw * t);
        }
    }



    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _tcpService.Close();
        base.OnFormClosed(e);
    }
}
