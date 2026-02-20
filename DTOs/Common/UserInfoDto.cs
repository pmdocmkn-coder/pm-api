namespace Pm.DTOs.Common
{
    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhotoUrl { get; set; }
        public string? EmployeeId { get; set; }
        public string? Division { get; set; }
    }
}
