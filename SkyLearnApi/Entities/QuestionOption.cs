namespace SkyLearnApi.Entities
{
    public class QuestionOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; } = null!;
        public string OptionText { get; set; } = string.Empty;
        public string? OptionTextAr { get; set; }
        public bool IsCorrect { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}
