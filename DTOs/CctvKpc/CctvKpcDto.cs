using System;
using Pm.DTOs.Common;

namespace Pm.DTOs.CctvKpc
{
    public class CctvKpcDto
    {
        public int Id { get; set; }
        public string Severity { get; set; } = "Low";
        public string Camera { get; set; } = null!;
        public string? IpCamera { get; set; }
        public string? Model { get; set; }
        public string? Brand { get; set; }
        public string? ExplicitLocation { get; set; }
        public string? FotoKoordinat { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCctvKpcDto
    {
        public string Severity { get; set; } = "Low";
        public string Camera { get; set; } = null!;
        public string? IpCamera { get; set; }
        public string? Model { get; set; }
        public string? Brand { get; set; }
        public string? ExplicitLocation { get; set; }
        public string? FotoKoordinat { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCctvKpcDto : CreateCctvKpcDto { }

    public class CctvKpcQueryDto : BaseQueryDto
    {
        public string? Severity { get; set; }
        public string? Brand { get; set; }
        public bool? IsActive { get; set; }
        public new int PageSize { get; set; } = 50;
    }
}
