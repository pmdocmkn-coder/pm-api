namespace Pm.Services
{
    public interface ITelegramService
    {
        Task<bool> SendDocumentExpiryMessageAsync(string chatId, string documentName, int daysRemaining, DateTime validUntil, string? fileLink, string documentId);

        /// <summary>
        /// Kirim 1 notifikasi WA yang merangkum beberapa dokumen dalam 1 grup yang sama.
        /// </summary>
        Task<bool> SendGroupedDocumentExpiryMessageAsync(
            string chatId,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<(string Name, DateTime ValidUntil)> documents);

        Task<bool> SendDocumentAnniversaryMessageAsync(
            string chatId, 
            string documentName, 
            int daysRemaining, 
            DateTime validUntil, 
            string? fileLink, 
            string documentId, 
            string documentType);

        Task<bool> SendBhpPaymentConfirmationAsync(
            string chatId,
            string documentName,
            int year,
            string invoiceNumber,
            string paidByUserName,
            bool isAllPaid,
            int paidCount,
            int totalCount);

        /// <summary>
        /// Kirim reminder peringatan BHP tahunan dengan detail tahun yang belum dibayar.
        /// </summary>
        Task<bool> SendBhpPaymentReminderAsync(
            string chatId,
            string documentName,
            int daysToAnniv,
            int currentYear,
            IEnumerable<(int Year, bool IsPaid, string? InvoiceNumber)> bhpItems);

        /// <summary>
        /// Kirim reminder BHP tahunan untuk grup dokumen ISR.
        /// </summary>
        Task<bool> SendGroupedBhpPaymentReminderAsync(
            string chatId,
            string groupName,
            int daysToAnniv,
            int currentYear,
            IEnumerable<(string DocName, int UnpaidCount, IEnumerable<int> UnpaidYears)> groupItems);

        Task<bool> SendGroupedDocumentAnniversaryMessageAsync(
            string chatId,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<(string Name, string Type)> documents);
    }
}
