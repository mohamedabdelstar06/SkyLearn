namespace SkyLearnApi.DTOs.Activities
{
    public class QuizGradeItem
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? ScorePercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
    }
}
