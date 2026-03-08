namespace SkyLearnApi.DTOs.Lectures
{
    public class CreateLectureDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IFormFile? File { get; set; }
        public List<IFormFile>? AdditionalFiles { get; set; }
    }
}
