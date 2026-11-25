# Silinder AR (Kelompok 5)

Aplikasi Android yang mengenali kaleng sebagai *cylinder target* lalu menampilkan objek AR, dan menyediakan ruang virtual tempat kaleng dapat diketuk, dipegang, diputar, dan dilepas dengan umpan balik getar.

## Mencoba aplikasi

Unduh dari [halaman Release](https://github.com/brahamrenatus-ai/tugas-silinder-ar/releases/tag/v1.0):

| Berkas | Untuk |
|---|---|
| `SilinderAR-v1.0.apk` | Aplikasi, Android 8.0 ke atas (ARM64) |
| `Marker-Kaleng-A4.pdf` | Marker untuk dicetak |
| `Marker-Kaleng-Layar.png` | Marker untuk dibuka di layar komputer |

1. Pasang APK, lalu izinkan kamera. Nama aplikasi di HP: TUGAS SILINDER TARGET AR.
2. Cetak `Marker-Kaleng-A4.pdf` pada A4 mendatar dengan **skala 100%** (garis cek harus 10 cm). Potong, gulung, lalu lem menjadi silinder berdiameter 6,6 cm. Tanpa mencetak: buka `Marker-Kaleng-Layar.png` di layar komputer.
3. Arahkan kamera ke marker dari jarak sekitar 40 cm.
4. Tekan **Explore Virtual Mode** untuk masuk ke ruang virtual, dan **Kembali** untuk ke mode AR.

## Fitur

| Mode | Aksi | Hasil |
|---|---|---|
| AR | Kaleng terdeteksi | Kaleng digital, logo 3D, kartu informasi, gelembung, dan naga |
| AR | Goyangkan kaleng, atau ketuk kaleng di layar | Naga mengaum, getar 120 ms |
| AR | Miringkan kaleng | Soda tertuang, getar 30 ms |
| Virtual | Ketuk | Kaleng menyala, getar 30 ms |
| Virtual | Tahan dan geser | Kaleng terangkat, mengikuti jari, dan berputar; getar 40 ms |
| Virtual | Lepas | Kaleng jatuh, getar 40 ms; membentur meja, getar 120 ms |

## Membuka proyek

- Unity **6000.6.10f1** dengan Android Build Support
- Universal AR by Zappar 3.2.10, Universal Render Pipeline 17.6.0, Input System 1.20.1
- Git harus terpasang, karena paket Zappar diambil dari GitHub saat proyek pertama kali dibuka
- Scene: `Assets/Scenes/SampleScene.unity` (AR) dan `Assets/Scenes/VirtualScene.unity` (Virtual Mode)

| Folder | Isi |
|---|---|
| `Assets/Scripts/AR` | Penyaring pose, naga, goyangan, tuangan, gelembung |
| `Assets/Scripts` | Interaksi sentuh, rotasi, fisika, dan benturan kaleng |
| `Assets/Scripts/Haptics` | Getaran |
| `Assets/Scripts/UI` | Perpindahan scene |
| `Assets/StreamingAssets` | Target hasil pelatihan (`.zpt`) |

## Anggota

| Nama | Bagian |
|---|---|
| Latifah Putri Fakhruddin | Lingkungan virtual |
| Ray Agung | AR, interaksi sentuh dan pegang |
| Abraham Renatus | Rotasi, fisika, penggabungan, dan penyesuaian AR |
| Galank Saputra | Haptic dan antarmuka |

Teknik Informatika, Universitas Tadulako
