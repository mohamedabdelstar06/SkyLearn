namespace SkyLearnApi.DTOs.Quizzes
{
    public class QuizStudentResultsDto
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public decimal TotalMarks { get; set; }
        public List<QuizResultResponseDto> Results { get; set; } = new();
    }
}
