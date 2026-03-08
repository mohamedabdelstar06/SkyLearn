namespace SkyLearnApi.DTOs.Quizzes
{
    public class UpdateQuizDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int? MaxAttempts { get; set; }
        public decimal? PassingScore { get; set; }
        public bool? ShuffleQuestions { get; set; }
        public bool? ShowCorrectAnswers { get; set; }
        public bool? ShowExplanations { get; set; }
        public string? GradingMode { get; set; }
        public DateTime? DueDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsVisible { get; set; }
    }
}
