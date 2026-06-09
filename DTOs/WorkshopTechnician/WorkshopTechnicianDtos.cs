namespace Pm.DTOs.WorkshopTechnician
{
    public class WorkshopTechnicianDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateWorkshopTechnicianDto
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateWorkshopTechnicianDto
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
