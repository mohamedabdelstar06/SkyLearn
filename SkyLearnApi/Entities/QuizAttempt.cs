namespace SkyLearnApi.Entities
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;
        public int StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;
        public int AttemptNumber { get; set; } = 1;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? ScorePercent { get; set; }
        public string Status { get; set; } = "InProgress"; // InProgress, Submitted, Graded, Abandoned
        public bool IsGraded { get; set; } = false;
        public int? GradedById { get; set; }
        public ApplicationUser? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }
        public int? TimeSpentSeconds { get; set; }

        // Navigation properties
        public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
    }
}
