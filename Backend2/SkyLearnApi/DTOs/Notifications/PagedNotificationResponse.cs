namespace SkyLearnApi.DTOs.Notifications
{
    public class PagedNotificationResponse
    {
        public List<NotificationResponseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int UnreadCount { get; set; }
    }
}
