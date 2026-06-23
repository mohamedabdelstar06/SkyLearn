namespace SkyLearnApi.DTOs.Quizzes
{
    public class GenerateQuizDto
    {
        public int? CourseId { get; set; }
        public List<int>? LectureIds { get; set; }
        public IFormFile? ImportedPdf { get; set; }
        public string? QuestionTypes { get; set; }
        public int NumberOfQuestions { get; set; } = 10;
        public string DifficultyLevel { get; set; } = "Medium";
        public string? CustomPrompt { get; set; }
        public string QuizScope { get; set; } = "Course";
        public int? TargetSquadronId { get; set; }
        public string? Title { get; set; }
    }
}
