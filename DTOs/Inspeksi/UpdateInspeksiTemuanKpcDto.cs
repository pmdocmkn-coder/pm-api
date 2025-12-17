// Pm/DTOs/UpdateInspeksiTemuanKpcDto.cs - UNTUK FORMDATA
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Pm.DTOs
{
    public class UpdateInspeksiTemuanKpcDto
    {
        // ✅ BASIC FIELDS - bisa string karena FormData
        public string? Ruang { get; set; }
        public string? Temuan { get; set; }
        public string? Severity { get; set; }
        public string? TanggalTemuan { get; set; } // STRING untuk FormData
        public string? Status { get; set; }

        // ✅ OPTIONAL TEXT FIELDS
        public string? KategoriTemuan { get; set; }
        public string? Inspector { get; set; }
        public string? NoFollowUp { get; set; }
        public string? PicPelaksana { get; set; }
        public string? PerbaikanDilakukan { get; set; }
        public string? Keterangan { get; set; }

        // ✅ DATE FIELDS - STRING untuk FormData
        public string? TanggalPerbaikan { get; set; }
        public string? TanggalSelesaiPerbaikan { get; set; }

        // ✅ CLEAR FLAGS - STRING karena FormData
        public string? ClearKategoriTemuan { get; set; }
        public string? ClearInspector { get; set; }
        public string? ClearNoFollowUp { get; set; }
        public string? ClearPicPelaksana { get; set; }
        public string? ClearPerbaikanDilakukan { get; set; }
        public string? ClearKeterangan { get; set; }
        public string? ClearTanggalPerbaikan { get; set; }
        public string? ClearTanggalSelesaiPerbaikan { get; set; }

        // ✅ FILE UPLOADS
        [FromForm(Name = "fotoTemuanFiles")]
        public List<IFormFile>? FotoTemuanFiles { get; set; }

        [FromForm(Name = "fotoHasilFiles")]
        public List<IFormFile>? FotoHasilFiles { get; set; }
    }
}