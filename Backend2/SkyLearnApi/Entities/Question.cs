namespace SkyLearnApi.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;
        public string QuestionText { get; set; } = string.Empty;
        public string? QuestionTextAr { get; set; }
        public string QuestionType { get; set; } = "MCQ"; // MCQ, Written, TrueFalse
        public decimal Marks { get; set; } = 1;
        public string DifficultyLevel { get; set; } = "Medium"; // Easy, Medium, Hard
        public string? Explanation { get; set; }
        public string? ExplanationAr { get; set; }
        public string? SourceReference { get; set; }
        public int SortOrder { get; set; } = 0;
        public string? ImageUrl { get; set; }

        // Navigation properties
        public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
}
