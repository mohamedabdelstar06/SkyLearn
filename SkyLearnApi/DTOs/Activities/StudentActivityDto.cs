namespace SkyLearnApi.DTOs.Activities
{
    public class StudentActivityDto
    {
        public int ActivityId { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DeadLineDate { get; set; }
        public string Status { get; set; } = "NotStarted";
        public decimal ProgressPercent { get; set; }
        public bool IsViewed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
