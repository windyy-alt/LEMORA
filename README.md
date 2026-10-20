# Lemora — Your Personal Digital Wardrobe

> **Evaluasi Tengah Semester (ETS) - Pemrograman Berbasis Kerangka Kerja (PBKK)**
> 
> **Tim Penyusun:**
> * Dilbina Windi Azahra (5025241180)
> * Dyah Utami Kesuma Dewi (5025241186)
> 
> **Kelas:** PBKK D

**Tautan Penting:**
PRD: https://drive.google.com/file/d/1rx5r2mb2OVbJRIQzQqYsVd6YniQQGv9I/view?usp=drive_link
Source Code:
Video Demo:

---

## 1. Tentang Lemora
**Lemora** adalah aplikasi desktop berbasis Windows yang dirancang untuk menyelesaikan permasalahan klasik: *"Aku punya banyak baju, tapi bingung mau pakai apa dan susah nyarinya."* 

Melalui pendekatan **Rich User Interface (Rich UI)**, Lemora bukan sekadar aplikasi pencatat (CRUD) biasa. Aplikasi ini mengintegrasikan katalog visual, *mix-and-match* outfit, penjadwalan kalender, hingga rekomendasi gaya cerdas berbasis aturan (*rule-based*) ke dalam satu ekosistem yang mulus. Semua data disimpan secara lokal dan aman, memastikan privasi pengguna terjaga tanpa perlu koneksi internet.

---

## 2. Fitur Utama (7 Modul Inti)

Aplikasi ini dibagi menjadi 7 modul utama yang saling terintegrasi:
1. **Dashboard**: Ringkasan metrik aktual dari database (total pakaian, jadwal hari ini, grafik kategori).
2. **My Closet**: Katalog visual. Mendukung operasi *Create, Read, Update, Delete* (CRUD), pencarian *case-insensitive* (nama/warna/brand), filter kategori, dan unggah foto pakaian.
3. **Outfit Studio**: Fitur *mix-and-match* dengan 6 slot (Top, Bottom, Dress, Outerwear, Shoes, Accessories) yang dilengkapi validasi cerdas anti-salah-kombinasi.
4. **Outfit Planner**: Kalender interaktif untuk merencanakan pakaian. Mencegah adanya jadwal ganda (1 hari = 1 jadwal).
5. **Style Suggestions**: Mesin rekomendasi outfit. Mencari kombinasi terbaik dari lemari pengguna berdasarkan jenis kegiatan (Campus, Formal, dll) dan preferensi warna.
6. **Wardrobe Insights**: Visualisasi data koleksi. Menampilkan grafik donat (distribusi kategori), rasio Top-to-Bottom, dan analisis warna.
7. **Settings & Themes**: Pengaturan tema **Light/Dark Mode** yang tersimpan secara permanen antar sesi, serta informasi lokasi *database*.

---

## 3. Arsitektur & Teknologi

Lemora dibangun dengan prinsip *Clean Architecture* (pemisahan *concern* yang tegas) menggunakan pola **MVVM (Model-View-ViewModel)**.

| Komponen | Teknologi |
| :--- | :--- |
| **Bahasa & Runtime** | C# / .NET 10 (`net10.0-windows`) |
| **Framework UI** | WPF (Windows Presentation Foundation) / XAML |
| **Pola Arsitektur** | MVVM (menggunakan `CommunityToolkit.Mvvm`) |
| **Database** | SQLite (Lokal) |
| **ORM / Akses Data** | Entity Framework Core (EF Core) |
| **Penyimpanan Aset** | Local File System (`%LOCALAPPDATA%\LEMORA`) |

**Alur Data (Data Flow):**
```
[View/XAML] ──(Data Binding)──▶ [ViewModel] ──(Commands)──▶ [Services] ──(EF Core)──▶ [SQLite DB]
```
*   **View**: Hanya mengurus tampilan (XAML). Tidak ada *business logic* di sini.
*   **ViewModel**: Menyimpan *state* (contoh: teks pencarian) dan meneruskan aksi ke Service.
*   **Services**: Berisi logika bisnis murni (`OutfitService`, `StyleSuggestionService`, dll).
*   **Data**: `LemoraDbContext` mengurus relasi tabel ke SQLite.

---

## 4. Deep Dive: Logika Bisnis (The Brains Behind Lemora)

Ini adalah bagian di mana Lemora melakukan "kalkulasi" di balik layar:

### A. Validasi Komposisi Outfit (`OutfitService.cs`)
Sistem tidak akan membiarkan pengguna menyimpan outfit yang berantakan. Aturan validasinya:
*   Wajib memiliki **Top + Bottom**, ATAU
*   Memiliki **Dress** (Dress dilarang digabung dengan Top/Bottom).
*   Satu item fisik tidak bisa dipakai di dua slot sekaligus (misal: 1 Kemeja dipakai sebagai Top dan Outerwear).

### B. Algoritma Rekomendasi Gaya (`StyleSuggestionService.cs`)
Sistem menghasilkan maksimal 3 outfit terbaik tanpa AI. Caranya:
1.  Mengambil semua pakaian dari *database*.
2.  Membentuk kombinasi dasar (Top+Bottom atau Dress).
3.  Memberikan **Scoring** (skor) pada setiap kombinasi:
    *   `+12 poin` jika `OccasionTags` pakaian cocok dengan acara yang dipilih (misal: tag "Formal" untuk acara "Formal").
    *   `+6 poin` jika warna pakaian masuk ke dalam *Color Family* preferensi (misal: *Warm Neutrals* mendeteksi warna "Beige", "Brown", "Cream").
    *   Menggunakan algoritma *deterministic sorting* untuk memastikan hasil rekomendasi selalu konsisten.

### C. Penyimpanan Tema Dinamis (`ThemeService.cs`)
Tema tidak di-*hardcode*. Saat pengguna mengganti tema, aplikasi menulis file `theme.txt` di folder lokal. Saat aplikasi dibuka, `ThemeService` membaca file tersebut dan menimpa `SolidColorBrush` di `App.xaml` secara *real-time*.

---

## 5. Skema Database (Entity Relationship)

Database dikelola sepenuhnya via EF Core Migrations. Terdapat 4 tabel utama:

1.  **`ClothingItems`**: Menyimpan data master pakaian (Id, Name, Category, Brand, ColorName, ImagePath, dll).
2.  **`Outfits`**: Menyimpan data outfit yang telah disusun (Id, Name, Occasion).
3.  **`OutfitItems`**: Tabel pivot *Many-to-Many* antara Outfit dan Pakaian. Memiliki **Unique Index** pada `(OutfitId, Slot)` untuk memastikan satu outfit hanya punya satu Top, satu Bottom, dst.
4.  **`OutfitPlans`**: Menyimpan kalender jadwal. Memiliki **Unique Constraint** pada kolom `Date` untuk mencegah jadwal ganda di hari yang sama.

---

## 6. UI/UX & Design System

Menerapkan standar **Rich UI**, Lemora menggunakan palet warna khusus yang didefinisikan di `App.xaml`:
*   **Primary:** Veranda Blue (`#6BB1AD`)
*   **Background (Light):** Lychee (`#EDECDB`) | **Background (Dark):** Slate Charcoal (`#151F23`)
*   **Accent:** Cupid Pink (`#E6748E`), Melon (`#E5A9A9`)
*   **Typography:** Font *Georgia* untuk judul (kesan elegan/editorial) dan *Segoe UI* untuk teks isi.
*   **Interaksi:** Custom `ControlTemplate` untuk tombol (efek hover/press), *Card-based layout*, dan *Empty State* yang komunikatif.

---

## 7. Struktur Direktori Project

```text
LEMORA/
├── Data/
│   └── LemoraDbContext.cs       # Konfigurasi EF Core & path database SQLite
├── Models/                      # Entity classes (ClothingItem, Outfit, dll)
├── Services/                    # Logika bisnis & I/O
│   ├── ClothingService.cs
│   ├── OutfitService.cs
│   ├── OutfitPlanService.cs
│   ├── StyleSuggestionService.cs
│   ├── ImageStorageService.cs
│   └── ThemeService.cs
├── ViewModels/                  # State, Properties, & Commands (MVVM)
├── Views/                       # UserControls UI (XAML)
│   ├── MyClosetView.xaml
│   ├── OutfitStudioView.xaml
│   ├── OutfitPlannerView.xaml
│   ├── StyleSuggestionsView.xaml
│   ├── WardrobeInsightsView.xaml
│   └── SettingsView.xaml
├── Migrations/                  # Riwayat pembuatan skema database EF Core
├── App.xaml                     # Design System (Colors, Styles, Brushes)
├── MainWindow.xaml              # Shell navigasi (Sidebar + Header)
└── LEMORA.csproj
```

---

## 8. Panduan Instalasi & Menjalankan

### Prasyarat
*   OS: **Windows 10 / 11** (WPF tidak mendukung Linux/macOS).
*   **.NET 10 SDK** (Bisa dicek via `dotnet --list-sdks`).
*   EF Core CLI Tools: `dotnet tool install --global dotnet-ef`.

### Langkah-langkah
1. **Clone repositori ini:**
   ```
   git clone https://github.com/username/LEMORA.git
   cd LEMORA
   ```

2. **Restore paket NuGet:**
   ```
   dotnet restore
   ```

3. **Inisialisasi Database:**
   Jika ini pertama kali aplikasi dijalankan, kamu wajib melakukan *migration* agar tabel terbuat di SQLite. Lewatkan langkah ini dan aplikasi akan *crash*.
   ```
   dotnet ef database update
   ```
   *(Database otomatis terbuat di `%LOCALAPPDATA%\LEMORA\lemora.db`).*

4. **Jalankan Aplikasi:**
   ```
   dotnet run
   ```

---

## 9. Pengujian (Manual Test Cases)

Pengujian dilakukan secara manual berdasarkan skenario di PRD:

| ID | Skenario | Hasil yang Diharapkan |
| :--- | :--- | :--- |
| **TC-01** | Jalankan aplikasi dengan DB kosong | Dashboard menampilkan angka 0, muncul *Empty State*. |
| **TC-02** | Tambah pakaian dengan data valid & foto | Item muncul di katalog My Closet, data tersimpan setelah restart. |
| **TC-03** | Tambah pakaian tanpa nama | Tombol simpan ditolak, muncul pesan *"Clothing name is required"*. |
| **TC-07** | Buat outfit Top + Bottom | Outfit berhasil disimpan dan muncul di daftar Saved Outfits. |
| **TC-08** | Simpan outfit tanpa Bottom/Dress | Sistem menolak dan menjelaskan aturan komposisi minimum. |
| **TC-12** | Hapus pakaian yang sedang dipakai outfit | Data pakaian **tidak** terhapus (diblokir relasi *foreign key* EF Core). |
| **TC-14** | Ubah ke Dark Mode, tutup, lalu buka lagi | Aplikasi tetap berada di Dark Mode (proses *read/write* `theme.txt`). |

---

## 10. Batasan Aplikasi
*   Aplikasi berjalan secara **offline** dan *single-user* (tidak ada autentikasi/login).
*   Rekomendasi gaya murni menggunakan *rule-based logic* (kategori & warna), bukan AI generatif atau analisis gambar.
*   Karena menggunakan WPF, aplikasi ini eksklusif hanya untuk sistem operasi Windows.

---
