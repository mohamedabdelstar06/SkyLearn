namespace SkyLearnApi.DTOs.Quizzes
{
    public class CreateQuizDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int MaxAttempts { get; set; } = 1;
        public decimal? PassingScore { get; set; }
        public bool ShuffleQuestions { get; set; } = false;
        public bool ShowCorrectAnswers { get; set; } = true;
        public bool ShowExplanations { get; set; } = true;
        public string GradingMode { get; set; } = "Auto";
        public DateTime? DueDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public string? DifficultyLevel { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public List<CreateQuestionDto>? Questions { get; set; }
    }
}
