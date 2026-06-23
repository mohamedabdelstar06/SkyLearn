namespace SkyLearnApi.DTOs.Quizzes
{
    public class QuestionTakeDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? QuestionTextAr { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public decimal Marks { get; set; }
        public int SortOrder { get; set; }
        public string? ImageUrl { get; set; }
        public List<OptionTakeDto> Options { get; set; } = new();
    }
}
