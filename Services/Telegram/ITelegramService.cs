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

        Task<bool> SendGroupedDocumentAnniversaryMessageAsync(
            string chatId,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<(string Name, string Type)> documents);
    }
}
