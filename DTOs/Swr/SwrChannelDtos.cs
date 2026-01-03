using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class SwrChannelListDto
    {
        public int Id { get; set; }
        public string ChannelName { get; set; } = null!;
        public int SwrSiteId { get; set; }
        public string SwrSiteName { get; set; } = null!;
        public string SwrSiteType { get; set; } = null!;
        public decimal ExpectedSwrMax { get; set; }
        public decimal? ExpectedPwrMax { get; set; }
    }

    public class SwrChannelCreateDto
    {
        [Required(ErrorMessage = "Nama channel wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama channel maksimal 100 karakter")]
        public string ChannelName { get; set; } = null!;

        [Required(ErrorMessage = "Site ID wajib diisi")]
        public int SwrSiteId { get; set; }

        [Range(1.0, 3.0, ErrorMessage = "Expected SWR Max harus antara 1.0 - 3.0")]
        public decimal ExpectedSwrMax { get; set; } = 1.5m;

        [Range(0, 200, ErrorMessage = "Expected PWR Max harus antara 0 - 200")]
        public decimal? ExpectedPwrMax { get; set; } = 100m;


        
    }

    public class SwrChannelUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama channel wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama channel maksimal 100 karakter")]
        public string ChannelName { get; set; } = null!;

        [Required(ErrorMessage = "Site ID wajib diisi")]
        public int SwrSiteId { get; set; }

        [Range(1.0, 3.0, ErrorMessage = "Expected SWR Max harus antara 1.0 - 3.0")]
        public decimal ExpectedSwrMax { get; set; }

        [Range(0, 200, ErrorMessage = "Expected PWR Max harus antara 0 - 200")]
        public decimal? ExpectedPwrMax { get; set; } = 100m;
    }
}