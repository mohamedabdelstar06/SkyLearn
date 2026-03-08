namespace SkyLearnApi.DTOs.Quizzes
{
    public class CreateQuestionDto
    {
        [Required]
        public string QuestionText { get; set; } = string.Empty;
        [Required]
        public string QuestionType { get; set; } = "MCQ";
        public decimal Marks { get; set; } = 1;
        public string DifficultyLevel { get; set; } = "Medium";
        public string? Explanation { get; set; }
        public string? SourceReference { get; set; }
        public int SortOrder { get; set; } = 0;
        public List<CreateQuestionOptionDto>? Options { get; set; }
    }
}
