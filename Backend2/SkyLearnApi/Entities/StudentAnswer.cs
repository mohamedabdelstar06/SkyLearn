namespace SkyLearnApi.Entities
{
    public class StudentAnswer
    {
        public int Id { get; set; }
        public int QuizAttemptId { get; set; }
        public QuizAttempt QuizAttempt { get; set; } = null!;
        public int QuestionId { get; set; }
        public Question Question { get; set; } = null!;
        public int? SelectedOptionId { get; set; }
        public QuestionOption? SelectedOption { get; set; }
        public string? WrittenAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal? MarksAwarded { get; set; }
        public string? InstructorFeedback { get; set; }
    }
}
