namespace SkyLearnApi.DTOs.Quizzes
{
    public class QuizTakeResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public decimal TotalMarks { get; set; }
        public int AttemptNumber { get; set; }
        public List<QuestionTakeDto> Questions { get; set; } = new();
    }
}
