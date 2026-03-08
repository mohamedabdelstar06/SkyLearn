namespace SkyLearnApi.DTOs.Assignments
{
    public class SubmitAssignmentDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
