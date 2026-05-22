using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("RadioHandoverPhotos")]
    public class RadioHandoverPhoto
    {
        [Key]
        public int Id { get; set; }

        public int RadioHandoverId { get; set; }
        [ForeignKey(nameof(RadioHandoverId))]
        public RadioHandover RadioHandover { get; set; } = null!;

        public int SortOrder { get; set; }

        [Required]
        [Column(TypeName = "longtext")]
        public string PhotoBase64 { get; set; } = null!;
    }
}
