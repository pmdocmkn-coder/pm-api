namespace Pm.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendDocumentExpiryMessageAsync(string phone, string documentName, int daysRemaining, DateTime validUntil, string? fileLink, string documentId);

        /// <summary>
        /// Kirim 1 notifikasi WA yang merangkum beberapa dokumen dalam 1 grup yang sama.
        /// </summary>
        Task<bool> SendGroupedDocumentExpiryMessageAsync(
            string phone,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<string> documentNames);

        Task<bool> SendDocumentAnniversaryMessageAsync(
            string phone, 
            string documentName, 
            int daysRemaining, 
            DateTime validUntil, 
            string? fileLink, 
            string documentId, 
            string documentType);

        Task<bool> SendGroupedDocumentAnniversaryMessageAsync(
            string phone,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<(string Name, string Type)> documents);
    }
}
