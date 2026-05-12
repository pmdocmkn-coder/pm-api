using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class PmSiteOrderDto
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        public int OrderIndex { get; set; }
    }
}
