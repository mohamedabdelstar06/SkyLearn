namespace SkyLearnApi.DTOs.ActivityLog
{
    /// <summary>
    /// Response DTO for activity logs - matches the Moodle-style logs table.
    /// Columns: Time, UserFullName, EventContext, Component, EventName, Description, Origin, IpAddress
    /// Removed: Jti, SessionId, LoginTime, LogoutTime, SessionDurationSeconds, UserId
    /// </summary>
    public class ActivityLogResponseDto
    {
        public long Id { get; set; }
        public DateTime Time { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string? AffectedUser { get; set; }
        public string? EventContext { get; set; }
        public string? Component { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Origin { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// Paginated response wrapper for activity logs
    /// </summary>
    public class PagedActivityLogsResponseDto
    {
        public List<ActivityLogResponseDto> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
        public ActivityLogFilterOptions? FilterOptions { get; set; }
    }

    public class ActivityLogFilterOptions
    {
        public List<string> ActionNames { get; set; } = new();
        public List<string> Components { get; set; } = new();
        public List<ActivityLogUserOption> Users { get; set; } = new();
    }
    public class ActivityLogUserOption
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}
