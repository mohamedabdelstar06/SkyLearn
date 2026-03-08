namespace SkyLearnApi.Entities
{
    public class Quiz : Activity
    {
        public int? TimeLimitMinutes { get; set; }
        public int MaxAttempts { get; set; } = 1;
        public decimal? PassingScore { get; set; }
        public decimal TotalMarks { get; set; }
        public bool ShuffleQuestions { get; set; } = false;
        public bool ShowCorrectAnswers { get; set; } = true;
        public bool ShowExplanations { get; set; } = true;
        public bool IsAiGenerated { get; set; } = false;
        public string? AiPromptUsed { get; set; }
        public string GradingMode { get; set; } = "Auto"; // Auto, Manual, Mixed
        public string QuizScope { get; set; } = "Course"; // Course, Personal
        public string? SourceLectureIds { get; set; } // JSON array [1,5,12]
        public string? DifficultyLevel { get; set; } // Easy, Medium, Hard, Mixed

        // Navigation properties
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    }
}
