namespace Pm.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink);
        Task SendTemuanCreatedEmailAsync(int temuanId, string ruang, string temuan, string picEmail);
    }
}