namespace SkyLearnApi.DTOs.Assignments
{
    public class GradeAssignmentDto
    {
        [Required]
        public decimal Grade { get; set; }
        public string? Feedback { get; set; }
    }
}
