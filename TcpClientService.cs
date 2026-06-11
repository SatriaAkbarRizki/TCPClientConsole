using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

public class TcpClientService
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    private  String serverTCP = "127.0.0.1";
    private  int portTCP = 8888;

    public string NameClient { get; private set; } = "";


    public event Action<string>? MessageReceived;

    public event Action? Disconnected;

    // ══════════════════════════════════════════════════════════════
    //  Login & Connect
    // ══════════════════════════════════════════════════════════════

    public (bool success, string nameClient, string historyJson, string errorMsg) Login(
         string email, string password)
    {
        try
        {
            _client = new TcpClient(serverTCP, portTCP);
            _stream = _client.GetStream();

            var data = new User { Email = email, Password = password };
            var json = JsonSerializer.Serialize(data);
            var buffer = Encoding.UTF8.GetBytes(json);
            _stream.Write(buffer, 0, buffer.Length);

            var responseBuffer = new byte[1024];
            var byteReads = _stream.Read(responseBuffer, 0, responseBuffer.Length);
            var response = Encoding.UTF8.GetString(responseBuffer, 0, byteReads);

            var serverRes = JsonSerializer.Deserialize<ServerResponse>(response);

            if (serverRes == null || serverRes.Status != "200")
            {
                _client.Close();
                return (false, "", "", serverRes?.Message ?? "Login gagal.");
            }

            NameClient = serverRes.Data["name"];

       
            var historyBuffer = new byte[4096];
            var historyBytesRead = _stream.Read(historyBuffer, 0, historyBuffer.Length);
            string historyJson = Encoding.UTF8.GetString(historyBuffer, 0, historyBytesRead);

            return (true, NameClient, historyJson, "");
        }
        catch (Exception ex)
        {
            return (false, "", "", $"Koneksi gagal: {ex.Message}");
        }
    }

    public void changeConfigServer (String? server, int? port)
    {
        serverTCP = server ?? serverTCP;
        portTCP = port ?? portTCP;
    }

    public List<MessageModel> ParseHistory(string historyJson)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<MessageModel>>(historyJson);
            return list ?? new List<MessageModel>();
        }
        catch (JsonException)
        {
            return new List<MessageModel>();
        }
    }

    public void SendMessage(string text)
    {
        if (_stream == null || string.IsNullOrEmpty(text)) return;

        string formatted = $"[{NameClient}]: {text}";
        byte[] buffer = Encoding.UTF8.GetBytes(formatted);
        _stream.Write(buffer, 0, buffer.Length);
    }

    public void StartListening()
    {
        if (_stream == null) return;

        var listenThread = new Thread(() =>
        {
            var buffer = new byte[1024];
            int bytesRead;
            try
            {
                while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string pesanMasuk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(pesanMasuk);
                }
            }
            catch
            {
                Disconnected?.Invoke();
            }
        });
        listenThread.IsBackground = true;
        listenThread.Start();
    }

    public void Close()
    {
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
    }
}
