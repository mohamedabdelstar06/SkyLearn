namespace SkyLearnApi.DTOs.Quizzes
{
    public class SubmitAnswerDto
    {
        [Required]
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? WrittenAnswer { get; set; }
    }
}
