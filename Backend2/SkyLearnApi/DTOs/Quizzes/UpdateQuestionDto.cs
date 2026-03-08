namespace SkyLearnApi.DTOs.Quizzes
{
    public class UpdateQuestionDto
    {
        public string? QuestionText { get; set; }
        public string? QuestionType { get; set; }
        public decimal? Marks { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? Explanation { get; set; }
        public string? SourceReference { get; set; }
        public int? SortOrder { get; set; }
        public List<CreateQuestionOptionDto>? Options { get; set; }
    }
}
