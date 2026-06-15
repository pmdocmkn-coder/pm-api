namespace Pm.DTOs.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int? RecipientUserId { get; set; }
        public string? RecipientRoleName { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Category { get; set; }
        public string? LinkUrl { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        public int? RecipientUserId { get; set; }
        public string? RecipientRoleName { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Category { get; set; }
        public string? LinkUrl { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
    }
}
