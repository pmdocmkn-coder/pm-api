using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pm.Data;
using Pm.Models;

namespace Pm.Services.Telegram
{
    public class TelegramQueueService : ITelegramQueueService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TelegramQueueService> _logger;

        public TelegramQueueService(AppDbContext context, ILogger<TelegramQueueService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task EnqueueMessageAsync(string ChatId, string message)
        {
            if (string.IsNullOrWhiteSpace(ChatId) || string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Invalid ChatId or message provided for queue.");
                return;
            }

            try
            {
                var chatIds = ChatId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var id in chatIds)
                {
                    var queueItem = new TelegramQueue
                    {
                        ChatId = id,
                        Message = message,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TelegramQueues.Add(queueItem);
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Message to {ChatId} successfully queued to {Count} IDs.", ChatId, chatIds.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue message to {ChatId}", ChatId);
            }
        }
    }
}
