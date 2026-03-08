namespace SkyLearnApi.DTOs.Quizzes
{
    public class OptionTakeDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public string? OptionTextAr { get; set; }
        public int SortOrder { get; set; }
    }
}
