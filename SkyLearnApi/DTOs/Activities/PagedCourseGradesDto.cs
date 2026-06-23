namespace SkyLearnApi.DTOs.Activities
{
    public class StudentCourseGradeDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int TotalLectures { get; set; }
        public int ViewedLectures { get; set; }
        public decimal LectureProgressPercent { get; set; }
        public List<QuizGradeItem> QuizGrades { get; set; } = new();

        // Assignment grades
        public List<AssignmentGradeItem> AssignmentGrades { get; set; } = new();
    }

    public class PagedCourseGradesDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<StudentCourseGradeDto> Students { get; set; } = new();
    }
}
