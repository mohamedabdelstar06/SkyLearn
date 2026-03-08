namespace SkyLearnApi.Entities
{
    public class StudentActivityProgress
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public Activity Activity { get; set; } = null!;
        public int StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;
        public string Status { get; set; } = "NotStarted"; // NotStarted, InProgress, Completed
        public decimal ProgressPercent { get; set; } = 0;
        public DateTime? FirstAccessedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public long TotalTimeSpentSeconds { get; set; } = 0;
        public DateTime? CompletedAt { get; set; }
    }
}
