namespace SkyLearnApi.DTOs.Activities
{
    public class AssignmentGradeItem
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public decimal? Grade { get; set; }
        public decimal MaxGrade { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
    }
}
