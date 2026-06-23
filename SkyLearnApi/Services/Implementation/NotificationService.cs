using Microsoft.AspNetCore.SignalR;
using SkyLearnApi.DTOs.Notifications;
using SkyLearnApi.Hubs;
using Hangfire;

namespace SkyLearnApi.Services.Implementation
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public NotificationService(
            AppDbContext context,
            IHubContext<NotificationHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationService> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<PagedNotificationResponse> GetUserNotificationsAsync(int userId, NotificationFilterParams filter)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .AsQueryable();

            if (filter.IsRead.HasValue)
                query = query.Where(n => n.IsRead == filter.IsRead.Value);

            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(n => n.Type == filter.Type);

            var totalCount = await query.CountAsync();
            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Body = n.Body,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    ReferenceActivityId = n.ReferenceActivityId,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new PagedNotificationResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                UnreadCount = unreadCount
            };
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Push updated unread count to the user in real-time
            var unreadCount = await GetUnreadCountAsync(userId);
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("UnreadCountUpdated", unreadCount);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();
                await activityService.TrackEntityActionAsync(ActivityActions.NotificationRead, "Notification", notificationId, userId,
                    $"Notification '{notification.Title}' marked as read");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to track notification read"); }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            await _context.SaveChangesAsync();

            // Push updated unread count (0) to the user in real-time
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("UnreadCountUpdated", 0);
        }

        public async Task CreateNotificationAsync(int userId, string title, string body, string type, int? referenceActivityId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                ReferenceActivityId = referenceActivityId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Push notification to user in real-time via SignalR
            var dto = new NotificationResponseDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                IsRead = false,
                ReferenceActivityId = notification.ReferenceActivityId,
                CreatedAt = notification.CreatedAt
            };

            var unreadCount = await GetUnreadCountAsync(userId);

            // Send both the notification data and the updated unread count
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveNotification", dto);
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("UnreadCountUpdated", unreadCount);

            _logger.LogDebug("Notification pushed to user {UserId} via SignalR: {Title}", userId, title);

            // Schedule delayed email notification via Hangfire (10 minutes delay)
            _backgroundJobClient.Schedule<EmailJobs>(
                jobs => jobs.SendDelayedEmailNotificationAsync(notification.Id),
                TimeSpan.FromMinutes(10));
        }

        public async Task CreateBulkNotificationsAsync(List<int> userIds, string title, string body, string type, int? referenceActivityId = null)
        {
            var notifications = userIds.Select(uid => new Notification
            {
                UserId = uid,
                Title = title,
                Body = body,
                Type = type,
                ReferenceActivityId = referenceActivityId
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Push to each user individually via their SignalR group
            foreach (var notification in notifications)
            {
                var dto = new NotificationResponseDto
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Body = notification.Body,
                    Type = notification.Type,
                    IsRead = false,
                    ReferenceActivityId = notification.ReferenceActivityId,
                    CreatedAt = notification.CreatedAt
                };

                var unreadCount = await GetUnreadCountAsync(notification.UserId);

                await _hubContext.Clients.Group($"user_{notification.UserId}")
                    .SendAsync("ReceiveNotification", dto);
                await _hubContext.Clients.Group($"user_{notification.UserId}")
                    .SendAsync("UnreadCountUpdated", unreadCount);
            }

            _logger.LogDebug("Bulk notifications pushed to {Count} users via SignalR", userIds.Count);

            // Schedule delayed email notifications via Hangfire (10 minutes delay) for each user
            foreach (var notification in notifications)
            {
                _backgroundJobClient.Schedule<EmailJobs>(
                    jobs => jobs.SendDelayedEmailNotificationAsync(notification.Id),
                    TimeSpan.FromMinutes(10));
            }
        }

        public async Task NotifyEnrolledStudentsAsync(int courseId, string title, string body, string type, int? referenceActivityId = null)
        {
            try
            {
                var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
                if (course == null)
                {
                    _logger.LogWarning("NotifyEnrolledStudents skipped: Course {CourseId} not found", courseId);
                    return;
                } var autoEnrolledIds = await _context.StudentProfiles
                    .Where(sp => sp.YearId == course.YearId && sp.DepartmentId == course.DepartmentId)
                    .Select(sp => sp.UserId)
                    .ToListAsync();

                // Manually enrolled students from other years
                var manualEnrolledIds = await _context.Enrollments
                    .Where(e => e.CourseId == courseId)
                    .Include(e => e.StudentProfile)
                    .Where(e => e.StudentProfile.YearId != course.YearId)
                    .Select(e => e.StudentProfile.UserId)
                    .ToListAsync();

                var allStudentIds = autoEnrolledIds.Concat(manualEnrolledIds).Distinct().ToList();

                if (!allStudentIds.Any())
                {
                    _logger.LogDebug("NotifyEnrolledStudents: No students enrolled in course {CourseId} '{CourseTitle}'",
                        courseId, course.Title);
                    return;
                }

                _logger.LogInformation(
                    "Sending notification to {Count} enrolled students in course {CourseId} '{CourseTitle}': {Title}",
                    allStudentIds.Count, courseId, course.Title, title);

                await CreateBulkNotificationsAsync(allStudentIds, title, body, type, referenceActivityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to notify enrolled students for course {CourseId}. Title: {Title}, Type: {Type}",
                    courseId, title, type);
                // Don't rethrow - notification failure shouldn't break the main operation
            }
        }
    }
}
