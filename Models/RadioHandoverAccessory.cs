using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("RadioHandoverAccessories")]
    public class RadioHandoverAccessory
    {
        [Key]
        public int Id { get; set; }

        public int RadioHandoverId { get; set; }
        [ForeignKey(nameof(RadioHandoverId))]
        public RadioHandover RadioHandover { get; set; } = null!;

        /// <summary>Legacy preset code (antenna, battery, …). Optional for manual rows.</summary>
        [MaxLength(50)]
        public string? AccessoryCode { get; set; }

        [Required, MaxLength(200)]
        public string ItemName { get; set; } = null!;

        public int Quantity { get; set; } = 1;

        [MaxLength(20)]
        public string Unit { get; set; } = "EA";

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? SerialNumber { get; set; }
    }
}
