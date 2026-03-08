namespace SkyLearnApi.DTOs.Quizzes
{
    public class SubmitQuizDto
    {
        [Required]
        public List<SubmitAnswerDto> Answers { get; set; } = new();
    }
}
