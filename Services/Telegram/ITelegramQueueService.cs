using System.Threading.Tasks;

namespace Pm.Services.Telegram
{
    public interface ITelegramQueueService
    {
        Task EnqueueMessageAsync(string ChatId, string message);
    }
}
