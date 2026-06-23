namespace SkyLearnApi.DTOs.Activities
{
    public class CalendarEventDto
    {
        public int ActivityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty; // "Quiz" or "Assignment"
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public DateTime DeadLineDate { get; set; }
        public DateTime? StartDate { get; set; }
        public bool IsCompleted { get; set; } = false;
        public string ColorCode { get; set; } = "#3B82F6"; // e.g. blue for assignment, purple for quiz
    }
}
