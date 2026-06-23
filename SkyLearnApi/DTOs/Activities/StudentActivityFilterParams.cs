namespace SkyLearnApi.DTOs.Activities
{
    public class StudentActivityFilterParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 20;

        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
        public string? ActivityType { get; set; }
        public string? Status { get; set; }
        public int? CourseId { get; set; }
        public string? Search { get; set; }
    }
}
