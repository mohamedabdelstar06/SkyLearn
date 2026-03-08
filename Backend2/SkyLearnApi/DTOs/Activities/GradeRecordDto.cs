namespace SkyLearnApi.DTOs.Activities
{
    public class GradeRecordDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public List<QuizGradeItem> QuizGrades { get; set; } = new();
        public List<AssignmentGradeItem> AssignmentGrades { get; set; } = new();
    }
}
