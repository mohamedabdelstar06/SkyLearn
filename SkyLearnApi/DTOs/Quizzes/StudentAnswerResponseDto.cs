namespace SkyLearnApi.DTOs.Quizzes
{
    public class StudentAnswerResponseDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public decimal Marks { get; set; }
        public string? SelectedOptionText { get; set; }
        public string? CorrectOptionText { get; set; }
        public string? WrittenAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal? MarksAwarded { get; set; }
        public string? InstructorFeedback { get; set; }
        public string? Explanation { get; set; }
        public string? ExplanationAr { get; set; }
        public string? SourceReference { get; set; }
    }
}
