namespace SkyLearnApi.DTOs.Assignments
{
    public class UpdateAssignmentDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public decimal? MaxGrade { get; set; }
        public bool? AllowLateSubmission { get; set; }
        public DateTime? DueDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public bool? IsVisible { get; set; }
        public List<IFormFile>? AssignmentFiles { get; set; }
    }
}
