using SkyLearnApi.DTOs.Notifications;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] NotificationFilterParams filter)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _notificationService.GetUserNotificationsAsync(UserId.Value, filter);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!UserId.HasValue) return Unauthorized();
            var count = await _notificationService.GetUnreadCountAsync(UserId.Value);
            return Ok(new { unreadCount = count });
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            await _notificationService.MarkAsReadAsync(id, UserId.Value);
            return Ok(new { message = "Notification marked as read." });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!UserId.HasValue) return Unauthorized();
            await _notificationService.MarkAllAsReadAsync(UserId.Value);
            return Ok(new { message = "All notifications marked as read." });
        }
    }
}
