namespace SkyLearnApi.DTOs.Quizzes
{
    public class QuizResultResponseDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? ScorePercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? TimeSpentSeconds { get; set; }
        public List<StudentAnswerResponseDto>? Answers { get; set; }
    }
}
