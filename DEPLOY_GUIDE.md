# Panduan Deploy Manual Backend .NET ke aaPanel (VPS)

Dokumen ini berisi panduan langkah demi langkah untuk melakukan deploy ulang backend **PM API (.NET 8)** secara manual ke VPS melalui **aaPanel**, jika GitHub Actions sedang terkendala atau kuota runner habis.

---

## 📌 Informasi Server
- **URL API Production:** `https://api.mknops.web.id`
- **Dashboard aaPanel:** `https://203.153.127.45:30407/`
- **Folder Deploy Server:** `/www/wwwroot/api.mknops.web.id/publish/`
- **Service Systemd:** `pm-api.service`
- **Web Server Reverse Proxy:** `httpd` (Apache)

> ⚠️ **PERINGATAN PENTING:**  
> Di dalam folder `/www/wwwroot/api.mknops.web.id/publish/` di server terdapat file konfigurasi:  
> **`appsettings.Production.json`**  
> **JANGAN PERNAH MENGHAPUS FILE INI**, karena berisi kredensial database & JWT production.

---

## 🚀 Langkah-Langkah Deploy

### Langkah 1: Build & Kompres di Komputer Lokal
Buka terminal **PowerShell** di folder project backend:

```powershell
# 1. Pindah ke folder Backend
cd "C:\Users\jupri.eka\CODE PM\Backend\pm-api"

# 2. Hapus sisa publish lama (jika ada)
Remove-Item -Recurse -Force "publish", "publish.zip" -ErrorAction SilentlyContinue

# 3. Publish untuk Linux x64 (Self-Contained)
dotnet publish ./Pm.csproj -c Release -r linux-x64 --self-contained true -o ./publish

# 4. Kompres hasil publish menjadi publish.zip
Compress-Archive -Path "publish\*" -DestinationPath "publish.zip" -Force
```

File zip siap upload akan berada di:  
📁 `C:\Users\jupri.eka\CODE PM\Backend\pm-api\publish.zip` (~59 MB).

---

### Langkah 2: Upload ke aaPanel
1. Buka browser dan login ke **aaPanel** (`https://203.153.127.45:30407/`).
2. Klik menu **Files** di sidebar sebelah kiri.
3. Masuk ke direktori:
   ```text
   /www/wwwroot/api.mknops.web.id/publish
   ```
4. Klik tombol **`Upload >`** di bagian atas menu.
5. Klik **Upload File** / **Add File**, lalu pilih file dari laptop Anda:
   ```text
   C:\Users\jupri.eka\CODE PM\Backend\pm-api\publish.zip
   ```
6. Jika muncul dialog *File Conflict Confirmation* (file duplikat):
   - Klik tombol hijau **`Overwrite`**.
7. Tunggu hingga upload mencapai **100%**, lalu tutup jendela upload.

---

### Langkah 3: Ekstrak (Extract) File di aaPanel
1. Di daftar file, cari file **`publish.zip`**.
2. Klik kanan pada file `publish.zip` → pilih **`Extract`** (atau klik tombol **Uncompress** di kolom Operation).
3. Pastikan kolom **Extract path** tetap mengarah ke:
   ```text
   /www/wwwroot/api.mknops.web.id/publish
   ```
4. Klik tombol hijau **`Confirm`**.
5. Tunggu proses ekstrak selesai (semua file binary baru otomatis menimpa file lama).
6. *(Opsional)* Klik kanan pada file `publish.zip` → **Delete** agar penyimpanan server tetap lega.

---

### Langkah 4: Pastikan Permission File Binary `Pm`
1. Di dalam folder `/www/wwwroot/api.mknops.web.id/publish/`, cari file bernama:
   **`Pm`** *(huruf P besar, m kecil, tanpa ekstensi .dll)*.
2. Klik kanan pada file **`Pm`** → pilih **`Permission`**.
3. Pastikan nilainya adalah **`755`** (Owner: Read+Write+Execute, Group: Read+Execute, Public: Read+Execute).
4. Klik tombol hijau **`Apply`**.

---

### Langkah 5: Restart Service di Server
1. Di sidebar sebelah kiri aaPanel, klik menu **`Terminal`** *(di bawah Account, di atas AI)*.
2. Jalankan perintah restart service `pm-api.service`:
   ```bash
   sudo systemctl restart pm-api.service
   ```
3. Cek status untuk memastikan service sudah aktif berjalan:
   ```bash
   sudo systemctl status pm-api.service
   ```
   *Pastikan muncul tulisan hijau: `Active: active (running)`.*
4. *(Opsional)* Jika perlu me-restart web server reverse proxy:
   ```bash
   sudo systemctl restart httpd
   ```

---

### Langkah 6: Verifikasi Aplikasi
1. Buka website dashboard di browser:  
   👉 **`https://pm.mknops.web.id/#`**
2. Lakukan login dan uji coba modul yang baru saja diperbarui.
3. Buka Console Browser (F12) atau periksa apakah data termuat dengan sempurna tanpa error.
