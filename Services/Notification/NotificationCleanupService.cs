using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pm.Services.Notification
{
    public class NotificationCleanupService(IServiceProvider serviceProvider, ILogger<NotificationCleanupService> logger) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<NotificationCleanupService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        // Reset setiap minggu (7 hari) sesuai request
                        await notificationService.DeleteOldNotificationsAsync(7);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Notification Cleanup");
                }

                // Tunggu 24 jam sebelum pembersihan berikutnya
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Notification Cleanup Service is stopping.");
        }
    }
}
