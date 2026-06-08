using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

public class TCPClients
{   
    private  String _nameClient;
    public void start()
    {
        const String server = "127.0.0.1";
        const int port = 8888;

        using var client = new TcpClient(server, port);
        var stream  = client.GetStream();

        
        Console.WriteLine("Connected to server. Type messages to send:");

        
        var data = new User();

        while (true)
        {
            Console.Write("Email:");
            
            data.Email = Console.ReadLine() ?? "";

            if (data.Email == "exit" || data.Email == "ex") return;

            Console.Write("Pass:");
            data.Password = Console.ReadLine() ?? "";
            
            if (data.Email != "" && data.Password != ""){
                var json = JsonSerializer.Serialize(data);
                var buffer = Encoding.UTF8.GetBytes(json);
                stream.Write(buffer, 0, buffer.Length);

                var responseBuffer = new Byte[1024];
                var byteReads = stream.Read(responseBuffer, 0, responseBuffer.Length);
                var response = Encoding.UTF8.GetString(responseBuffer, 0, byteReads);
                Console.WriteLine($"Server Response: {response}");

                var serverRes = JsonSerializer.Deserialize<ServerResponse>(response);
                _nameClient = serverRes.Data["name"];

                if (serverRes != null && serverRes.Status == "200")
                {
                    Console.WriteLine($"\nSelamat Datang, {serverRes.Message}! Anda berhasil masuk ke ruang chat.");
                    Console.WriteLine("Ketik pesan Anda di bawah ini (ketik 'exit' untuk keluar):");
                    Console.WriteLine("=========================================================");
                    break;
                }
            }    
            else{
                    Console.WriteLine("Ensure not have empty");
            }
        }


        Thread listenThread = new Thread(() => readMessageFromServer(stream));
        listenThread.Start();

        while (true)
        {
            Console.Write("Pesan: ");
            string pesanChat = Console.ReadLine() ?? "";
            
            if (pesanChat.ToLower() == "exit") 
            {
                Console.WriteLine("Keluar dari aplikasi...");
                break;
            }

            if (!string.IsNullOrEmpty(pesanChat))
            {
                pesanChat = $"{_nameClient}: {pesanChat}";
                var chatBuffer = Encoding.UTF8.GetBytes(pesanChat);
                stream.Write(chatBuffer, 0, chatBuffer.Length);
            }
        }
    }

    private void readMessageFromServer(NetworkStream stream)
        {
            var buffer = new byte[1024];
            int bytesRead;

            try
            {

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string pesanMasuk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    
                    Console.WriteLine(pesanMasuk);
                }
            }
            catch
            {
    
                Console.WriteLine("\n[SISTEM] Koneksi ke server terputus.");
            }
        }

}