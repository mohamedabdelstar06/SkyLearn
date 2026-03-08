namespace SkyLearnApi.DTOs.Quizzes
{
    public class GradeAnswerDto
    {
        [Required]
        public int StudentAnswerId { get; set; }
        public decimal MarksAwarded { get; set; }
        public string? Feedback { get; set; }
    }
}
