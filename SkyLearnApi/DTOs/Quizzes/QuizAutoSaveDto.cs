namespace SkyLearnApi.DTOs.Quizzes
{
    public class QuizAutoSaveDto
    {
        public List<StudentAnswerSaveDto> Answers { get; set; } = new();
    }

    public class StudentAnswerSaveDto
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? WrittenAnswer { get; set; }
        public bool IsFlagged { get; set; } = false;
    }
}
