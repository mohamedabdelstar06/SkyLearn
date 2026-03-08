namespace SkyLearnApi.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = "General"; // QuizReminder, AssignmentDue, GradePublished, PasswordReset, General
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public bool EmailSent { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }
        public int? ReferenceActivityId { get; set; }
        public Activity? ReferenceActivity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
