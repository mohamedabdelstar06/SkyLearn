using SkyLearnApi.DTOs.Notifications;

namespace SkyLearnApi.Services.Interfaces
{
    public interface INotificationService
    {
        Task<PagedNotificationResponse> GetUserNotificationsAsync(int userId, NotificationFilterParams filter);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task CreateNotificationAsync(int userId, string title, string body, string type, int? referenceActivityId = null);
        Task CreateBulkNotificationsAsync(List<int> userIds, string title, string body, string type, int? referenceActivityId = null);
        Task NotifyEnrolledStudentsAsync(int courseId, string title, string body, string type, int? referenceActivityId = null);
    }
}

