namespace SkyLearnApi.DTOs.Quizzes
{
    public class GradeQuizDto
    {
        [Required]
        public List<GradeAnswerDto> Grades { get; set; } = new();
    }
}
