namespace SkyLearnApi.DTOs.ActivityLog
{
    /// <summary>
    /// Query parameters for filtering and paginating activity logs
    /// </summary>
    public class ActivityLogFilterParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? Search { get; set; }
        public string? ActionName { get; set; }
        public string? Component { get; set; }
        public int? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SortBy { get; set; } = "Time";
        public string SortDirection { get; set; } = "desc";
        public string? Origin { get; set; }
        public bool IncludeFilterOptions { get; set; } = false;
    }
}
