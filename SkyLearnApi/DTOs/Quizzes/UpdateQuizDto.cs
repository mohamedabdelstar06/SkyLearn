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
        public bool? ShuffleAnswers { get; set; }
        public DateTime? StartDate { get; set; }
        public bool? ShowCorrectAnswers { get; set; }
        public bool? ShowExplanations { get; set; }
        public string? GradingMode { get; set; }
        public DateTime? DeadLineDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsVisible { get; set; }
        public List<UpdateQuizQuestionDto>? Questions { get; set; }
    }

    public class UpdateQuizQuestionDto
    {
        public int? Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MCQ";
        public decimal Marks { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? Explanation { get; set; }
        public string? SourceReference { get; set; }
        public int SortOrder { get; set; }
        public List<UpdateQuizOptionDto>? Options { get; set; }
    }

    public class UpdateQuizOptionDto
    {
        public int? Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int SortOrder { get; set; }
    }
}
