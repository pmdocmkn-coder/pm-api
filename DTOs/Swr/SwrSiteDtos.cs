using System.ComponentModel.DataAnnotations;
using Pm.Enums;

namespace Pm.DTOs
{
    public class SwrSiteListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
        public string Type { get; set; } = null!; // "Trunking" or "Conventional"
        public int ChannelCount { get; set; }
    }

    public class SwrSiteCreateDto
    {
        [Required(ErrorMessage = "Nama site wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama site maksimal 100 karakter")]
        public string Name { get; set; } = null!;

        [StringLength(255, ErrorMessage = "Lokasi maksimal 255 karakter")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Tipe site wajib diisi")]
        public string Type { get; set; } = "Trunking"; // "Trunking" or "Conventional"
    }

    public class SwrSiteUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama site wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama site maksimal 100 karakter")]
        public string Name { get; set; } = null!;

        [StringLength(255, ErrorMessage = "Lokasi maksimal 255 karakter")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Tipe site wajib diisi")]
        public string Type { get; set; } = "Trunking";
    }
}