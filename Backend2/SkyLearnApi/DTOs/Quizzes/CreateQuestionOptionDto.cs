namespace SkyLearnApi.DTOs.Quizzes
{
    public class CreateQuestionOptionDto
    {
        [Required]
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}
