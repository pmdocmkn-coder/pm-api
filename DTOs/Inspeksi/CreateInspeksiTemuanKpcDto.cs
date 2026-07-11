using Microsoft.AspNetCore.Mvc;
using Pm.Helper;

public class CreateInspeksiTemuanKpcDto
{
    public required string Ruang { get; set; }
    public required string Temuan { get; set; }
    public string? KategoriTemuan { get; set; }
    public string? Inspector { get; set; }
    public string Severity { get; set; } = "Medium";
    public string TanggalTemuan { get; set; } = WitaHelper.Today.ToString("yyyy-MM-dd");
    public string? NoFollowUp { get; set; }
    public string? PicPelaksana { get; set; }
    public string? Keterangan { get; set; }

    [FromForm(Name = "fotoTemuanFiles")]
    public List<IFormFile>? FotoTemuanFiles { get; set; }
}