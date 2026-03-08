namespace SkyLearnApi.DTOs.Assignments
{
    public class CreateAssignmentDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public decimal MaxGrade { get; set; } = 100;
        public bool AllowLateSubmission { get; set; } = false;
        public DateTime? DueDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public bool IsVisible { get; set; } = true;
        public List<IFormFile>? AssignmentFiles { get; set; }
    }
}
