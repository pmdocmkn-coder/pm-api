namespace Pm.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink);
        Task SendTemuanCreatedEmailAsync(int temuanId, string ruang, string temuan, string picEmail);
        
        // Operational Document Expiry Notifications
        Task<bool> SendDocumentExpiryEmailAsync(string toEmail, string documentName, int daysRemaining, DateTime validUntil, string? fileLink, string documentId, string? documentType = null, string? groupName = null);
        Task<bool> SendGroupedDocumentExpiryEmailAsync(string toEmail, string groupName, int daysRemaining, DateTime validUntil, IEnumerable<(string DocumentName, DateTime ValidUntil)> documents);
        Task<bool> SendDocumentAnniversaryEmailAsync(string toEmail, string documentName, int daysRemaining, DateTime validUntil, string? fileLink, string documentId, string documentType);
        Task<bool> SendGroupedDocumentAnniversaryEmailAsync(string toEmail, string groupName, int daysRemaining, DateTime validUntil, IEnumerable<(string DocumentName, string DocumentType)> documents);
        Task<bool> SendBhpPaymentReminderEmailAsync(string toEmail, string documentName, int daysToAnniv, int currentYear, IEnumerable<(int Year, bool IsPaid, string? InvoiceNumber)> bhpItems);
        Task<bool> SendGroupedBhpPaymentReminderEmailAsync(string toEmail, string groupName, int daysToAnniv, int currentYear, IEnumerable<(string DocName, int UnpaidCount, IEnumerable<int> UnpaidYears)> groupItems);

        // Radio Repair & Handover Notifications
        Task<bool> SendRadioReadyForHelpdeskEmailAsync(string toEmail, string ticketNumber, string radioSerial, string equipmentName, string? unitNumber, string technicianName, string? notes, DateTime handoverAt, string webAppBaseUrl, bool isFromHelpdesk = false);
        Task<bool> SendTestNotificationEmailAsync(string toEmail);
    }
}