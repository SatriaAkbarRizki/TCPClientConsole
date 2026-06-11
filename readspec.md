# Dokumentasi Migrasi Proyek TCP Socket: Console ke GUI

Dokumentasi ini berfungsi sebagai panduan arsitektur dan cetak biru (*blueprint*) untuk memindahkan logika aplikasi *Client-Server* berbasis TCP yang sebelumnya berjalan di Terminal/Console ke dalam bentuk *Graphical User Interface* (GUI) menggunakan WinForms.

---

## 1. Konsep Arsitektur Sistem

Dalam migrasi ini, arsitektur sistem tetap menjaga pemisahan total antara **Server** dan **Client**. Laptop client tidak diizinkan untuk mengakses database secara langsung. Server bertindak sebagai satu-satunya jembatan data.

### Alur Komunikasi Utama
1. **Chat Real-Time**: Client A -> Kirim Teks via TCP -> Server -> Simpan ke DB -> Broadcast (`foreach`) via TCP -> Semua Client Online -> Tampil di UI Client masing-masing.
2. **Riwayat Chat (History)**: Client Baru Login -> Request Riwayat via TCP -> Server -> Query `SELECT` ke DB -> Ubah ke JSON Array -> Kirim via TCP -> Client Terima & Tampilkan di UI.

---

## 2. Tabel Pemetaan Komponen: Console vs GUI

Proses migrasi murni mengubah bagaimana cara input diambil dan bagaimana output ditampilkan di sisi Client. Logika jaringan TCP (`NetworkStream`) tetap sama.

| Fungsi pada Console | Komponen GUI / UI yang Cocok | Cara Kerja di UI |
| :--- | :--- | :--- |
| `Console.ReadLine()` (Email/Pass) | **TextBox** (`TxtEmail`, `TxtPassword`) | Mengambil nilai dari properti `.Text` saat tombol login diklik. |
| `Console.ReadLine()` (Input Chat) | **TextBox** (`TxtInputPesan`) | Tempat user mengetik pesan baru yang akan dikirim. |
| Tombol Enter (Kirim Data) | **Button** (`BtnLogin`, `BtnKirim`) | Event `Click` pada tombol memicu fungsi pengiriman biner TCP. |
| `Console.WriteLine()` (Teks Chat) | **ListBox** (`ChatListBox`) | Menampilkan deretan chat secara berbaris memanfaatkan `ObservableCollection`. |
| `Thread listenThread` | **Background Thread + Dispatcher** | Mendengarkan *stream* di latar belakang dan memperbarui UI secara aman via `Dispatcher`. |

---

## 3. Alur Eksekusi Antarmuka Client

Sistem pada Client direkomendasikan dibagi menjadi dua kondisi tampilan (bisa berupa dua halaman terpisah atau satu halaman dengan panel dinamis):

### Tahap 1: Kondisi Halaman Login
* User mengisi kotak input `TxtEmail` dan `TxtPassword`.
* Menekan tombol `BtnLogin` akan memicu serialisasi objek `User` menjadi JSON, lalu dikirim via `stream.Write`.
* Jalur utama menunggu respon status dari server. Jika server mengembalikan status `"200"`, simpan nama pengirim ke variabel `_nameClient`, tutup panel login, dan buka halaman utama chat.

### Tahap 2: Kondisi Halaman Chat Utama
* Begitu halaman terbuka, client langsung melakukan `stream.Read` untuk menangkap string JSON Array berisi riwayat pesan lama dari server.
* JSON riwayat di-deserialize menjadi `List<MessageModel>`, lalu setiap baris dimasukkan ke penampung list UI.
* Setelah riwayat tampil, jalankan *Background Thread* (`AmbilChatRealTimeFromServer`) untuk memantau pesan broadcast baru dari server selama aplikasi aktif.

---

## 4. Struktur Kerangka Kode C# (Code-Behind) UI Client

Berikut adalah struktur logika standar di sisi Client untuk mengontrol komponen visual GUI dan menghubungkannya dengan *stream* TCP yang sudah ada:

```csharp
using System;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

public partial class ChatWindow
{
    private NetworkStream _stream;
    private string _nameClient;
    
    // List pintar yang terhubung langsung sebagai ItemsSource pada ListBox di layar UI
    public ObservableCollection<string> ChatMessages { get; set; } = new ObservableCollection<string>();

    public ChatWindow(NetworkStream stream, string nameClient)
    {
        // Catatan: Sesuaikan fungsi inisialisasi komponen sesuai framework GUI yang digunakan
        // InitializeComponent(); 
        
        _stream = stream;
        _nameClient = nameClient;

        // Hubungkan list data dengan komponen visual ListBox
        // MyChatListBox.ItemsSource = ChatMessages;

        // Jalankan background thread untuk mendengarkan pesan broadcast real-time dari server
        Thread listenThread = new Thread(() => AmbilChatRealTimeFromServer());
        listenThread.Start();
    }

    // Pemicu aksi saat user menekan tombol "Kirim" di UI
    private void BtnKirim_Click(object sender, EventArgs e)
    {
        // Misal TxtInputPesan adalah nama komponen TextBox input chat
        string pesan = TxtInputPesan.Text; 
        if (!string.IsNullOrEmpty(pesan))
        {
            // Format pesan sebelum dikirim agar server dapat melakukan parsing dengan Regex
            string formatPesan = $"[{_nameClient}]: {pesan}";
            byte[] buffer = Encoding.UTF8.GetBytes(formatPesan);
            
            // Tembak ke server via TCP
            _stream.Write(buffer, 0, buffer.Length);

            // Kosongkan kembali kotak input chat di layar
            TxtInputPesan.Text = ""; 
        }
    }

    // Fungsi satpam yang berjalan di latar belakang (Background Thread)
    private void AmbilChatRealTimeFromServer()
    {
        var buffer = new byte[1024];
        int bytesRead;
        try
        {
            while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string pesanMasuk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // CATATAN: Gunakan mekanisme UI Thread milik framework masing-masing (misal Dispatcher)
                // agar pembaruan data tidak menyebabkan Thread Safety Violation (Crash).
                // Contoh (Avalonia UI):
                // Dispatcher.UIThread.Post(() => { ChatMessages.Add(pesanMasuk); });
            }
        }
        catch
        {
            // Penanganan jika koneksi ke server terputus di tengah jalan
        }
    }
}