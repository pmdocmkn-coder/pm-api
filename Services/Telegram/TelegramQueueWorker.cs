using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pm.Data;
using System.Text;

namespace Pm.Services.Telegram
{
    public class TelegramQueueWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramQueueWorker> _logger;
        private readonly Random _random = new Random();

        public TelegramQueueWorker(IServiceProvider serviceProvider, ILogger<TelegramQueueWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TelegramQueueWorker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                bool messageProcessed = false;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    var settings = configuration.GetSection("TelegramSettings").Get<TelegramSettings>() ?? new TelegramSettings();

                    if (!string.IsNullOrWhiteSpace(settings.BotToken))
                    {
                        var pendingMessage = await dbContext.TelegramQueues
                            .Where(q => q.Status == "Pending" || (q.Status == "Failed" && q.RetryCount < q.MaxRetry))
                            .OrderBy(q => q.CreatedAt)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (pendingMessage != null)
                        {
                            messageProcessed = true;
                            
                            pendingMessage.Status = "Processing";
                            await dbContext.SaveChangesAsync(stoppingToken);

                            bool isSent = await SendMessageAsync(httpClientFactory, settings, pendingMessage.ChatId, pendingMessage.Message, stoppingToken);

                            if (isSent)
                            {
                                pendingMessage.Status = "Sent";
                                pendingMessage.SentAt = DateTime.UtcNow;
                                pendingMessage.ErrorMessage = null;
                                _logger.LogInformation("Queue item {Id} successfully sent to Telegram Chat {ChatId}.", pendingMessage.Id, pendingMessage.ChatId);
                            }
                            else
                            {
                                pendingMessage.RetryCount++;
                                if (pendingMessage.RetryCount >= pendingMessage.MaxRetry)
                                {
                                    pendingMessage.Status = "Failed";
                                    pendingMessage.ErrorMessage = "Max retries reached.";
                                    _logger.LogWarning("Queue item {Id} failed permanently after {RetryCount} retries.", pendingMessage.Id, pendingMessage.RetryCount);
                                }
                                else
                                {
                                    pendingMessage.Status = "Pending";
                                    pendingMessage.ErrorMessage = "Failed to send, will retry.";
                                    _logger.LogInformation("Queue item {Id} failed, marked for retry ({RetryCount}/{MaxRetry}).", pendingMessage.Id, pendingMessage.RetryCount, pendingMessage.MaxRetry);
                                }
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                    else
                    {
                        // Prevent tight loop if token is not set
                         await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in TelegramQueueWorker.");
                }

                if (messageProcessed)
                {
                    // Delay to prevent flood limit from Telegram. Telegram limits are 30 messages per second, but let's be safe.
                    // Telegram allows up to 20 messages per minute to the same group. We can do 1-2 seconds delay.
                    int delaySeconds = _random.Next(2, 5);
                    _logger.LogDebug("TelegramQueueWorker sleeping for {DelaySeconds} seconds after processing a message.", delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
                else
                {
                    // If no message was found, wait 10 seconds before checking again
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task<bool> SendMessageAsync(IHttpClientFactory httpClientFactory, TelegramSettings settings, string chatId, string message, CancellationToken stoppingToken)
        {
            try
            {
                var client = httpClientFactory.CreateClient("telegram");
                
                var url = $"https://api.telegram.org/bot{settings.BotToken}/sendMessage";
                
                var payload = new
                {
                    chat_id = chatId,
                    text = message,
                    parse_mode = "Markdown"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, stoppingToken);
                var responseBody = await response.Content.ReadAsStringAsync(stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                _logger.LogWarning("Telegram API error for Chat ID {ChatId}. Status: {Status}, Body: {Body}", chatId, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Telegram API for Chat ID {ChatId}", chatId);
                return false;
            }
        }
    }
}
