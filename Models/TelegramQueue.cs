namespace Pm.Models
{
    public class TelegramQueue
    {
        public int Id { get; set; }
        
        public string ChatId { get; set; } = string.Empty;
        
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Pending, Processing, Sent, Failed
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        public int RetryCount { get; set; } = 0;
        
        public int MaxRetry { get; set; } = 3;
        
        public string? ErrorMessage { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? SentAt { get; set; }
    }
}
