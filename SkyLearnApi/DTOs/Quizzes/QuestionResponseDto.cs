namespace SkyLearnApi.DTOs.Quizzes
{
    public class QuestionResponseDto
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? QuestionTextAr { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public decimal Marks { get; set; }
        public string DifficultyLevel { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public string? SourceReference { get; set; }
        public int SortOrder { get; set; }
        public string? ImageUrl { get; set; }
        public List<QuestionOptionResponseDto> Options { get; set; } = new();
    }

    public class QuestionOptionResponseDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public string? OptionTextAr { get; set; }
        public bool IsCorrect { get; set; }
        public int SortOrder { get; set; }
    }
}
