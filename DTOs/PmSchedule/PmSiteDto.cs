using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class PmSiteDto
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!;
        
        public int OrderIndex { get; set; }
    }
}
