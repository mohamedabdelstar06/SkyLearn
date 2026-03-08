namespace SkyLearnApi.DTOs.Quizzes
{
    public class QuizResponseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int MaxAttempts { get; set; }
        public decimal? PassingScore { get; set; }
        public decimal TotalMarks { get; set; }
        public bool ShuffleQuestions { get; set; }
        public bool ShowCorrectAnswers { get; set; }
        public bool ShowExplanations { get; set; }
        public bool IsAiGenerated { get; set; }
        public string GradingMode { get; set; } = string.Empty;
        public string QuizScope { get; set; } = string.Empty;
        public string? DifficultyLevel { get; set; }
        public DateTime? DueDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public string? TargetSquadronName { get; set; }
        public int QuestionCount { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
