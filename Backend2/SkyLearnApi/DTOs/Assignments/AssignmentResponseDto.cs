namespace SkyLearnApi.DTOs.Assignments
{
    public class AssignmentResponseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public decimal MaxGrade { get; set; }
        public bool AllowLateSubmission { get; set; }
        public DateTime? DueDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public string? TargetSquadronName { get; set; }
        public int SubmissionCount { get; set; }
        public bool IsVisible { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AssignmentFileUrls { get; set; }
    }
}
