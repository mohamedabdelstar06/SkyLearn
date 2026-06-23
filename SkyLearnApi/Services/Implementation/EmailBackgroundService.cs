using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace SkyLearnApi.Services.Implementation
{
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

        public EmailBackgroundService(IServiceProvider serviceProvider, ILogger<EmailBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailBackgroundService started. Checking unread notifications every {Interval} minutes for email dispatch",
                _checkInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessUnreadNotifications(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("EmailBackgroundService stopping due to cancellation");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "EmailBackgroundService: Unhandled error processing notification emails. ExceptionType: {ExceptionType}, Message: {Message}",
                        ex.GetType().Name, ex.Message);
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("EmailBackgroundService stopped");
        }

        private async Task ProcessUnreadNotifications(CancellationToken stoppingToken)
        {
            var sw = Stopwatch.StartNew();

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var cutoff = DateTime.UtcNow.AddMinutes(-10);

            var pendingNotifications = await context.Notifications
                .Where(n => !n.IsRead && !n.EmailSent && n.CreatedAt <= cutoff)
                .Include(n => n.User)
                .Take(50) // Process in batches
                .ToListAsync(stoppingToken);

            if (!pendingNotifications.Any())
            {
                _logger.LogDebug("EmailBackgroundService: No pending notifications to email (cutoff: {Cutoff})", cutoff);
                return;
            }

            _logger.LogInformation(
                "EmailBackgroundService: Processing {Count} unread notifications for email dispatch (older than {Cutoff})",
                pendingNotifications.Count, cutoff);

            var successCount = 0;
            var failCount = 0;

            foreach (var notification in pendingNotifications)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var userEmail = notification.User?.Email;
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        _logger.LogWarning(
                            "EmailBackgroundService: Skipping notification {NotificationId} — user {UserId} has no email address",
                            notification.Id, notification.UserId);
                        notification.EmailSent = true; // Mark to avoid retrying
                        notification.EmailSentAt = DateTime.UtcNow;
                        continue;
                    }

                    var htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; color: white; border-radius: 8px 8px 0 0;'>
                                <h2 style='margin: 0;'>📚 SkyLearn Notification</h2>
                            </div>
                            <div style='padding: 20px; background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 0 0 8px 8px;'>
                                <h3 style='color: #1f2937;'>{notification.Title}</h3>
                                <p style='color: #4b5563; line-height: 1.6;'>{notification.Body}</p>
                                <div style='background: #eef2ff; padding: 12px; border-radius: 6px; margin-top: 12px;'>
                                    <p style='margin: 0; color: #4338ca; font-size: 14px;'>
                                        <strong>Type:</strong> {notification.Type}<br/>
                                        <strong>Sent at:</strong> {notification.CreatedAt:g} UTC
                                    </p>
                                </div>
                                <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 16px 0;'/>
                                <p style='color: #9ca3af; font-size: 12px;'>
                                    This email was sent because you have an unread notification on SkyLearn
                                    that has been waiting for over 10 minutes. Log in to view and respond.
                                </p>
                            </div>
                        </div>";

                    await emailService.SendEmailAsync(userEmail, $"SkyLearn: {notification.Title}", htmlBody);

                    notification.EmailSent = true;
                    notification.EmailSentAt = DateTime.UtcNow;
                    successCount++;

                    _logger.LogDebug(
                        "Email sent for notification {NotificationId} to {Email}. Title: {Title}, Type: {Type}",
                        notification.Id, userEmail, notification.Title, notification.Type);
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex,
                        "Failed to send email for notification {NotificationId} to user {UserId} ({Email}). " +
                        "Title: {Title}, Type: {Type}, ExceptionType: {ExceptionType}",
                        notification.Id, notification.UserId, notification.User?.Email ?? "null",
                        notification.Title, notification.Type, ex.GetType().Name);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
            sw.Stop();

            _logger.LogInformation(
                "EmailBackgroundService: Batch complete in {ElapsedMs}ms. Success: {Success}, Failed: {Failed}, Total: {Total}",
                sw.ElapsedMilliseconds, successCount, failCount, pendingNotifications.Count);
        }
    }
}

