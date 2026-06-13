namespace Pm.DTOs
{
    public class WorkshopTechnicianDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// ID User yang terkait dengan teknisi ini (nullable)
        /// </summary>
        public int? UserId { get; set; }
    }

    public class CreateWorkshopTechnicianDto
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// ID User yang terkait (opsional - untuk link teknisi dengan akun user)
        /// </summary>
        public int? UserId { get; set; }
    }

    public class UpdateWorkshopTechnicianDto
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        
        /// <summary>
        /// ID User yang terkait (opsional)
        /// </summary>
        public int? UserId { get; set; }
    }
}
