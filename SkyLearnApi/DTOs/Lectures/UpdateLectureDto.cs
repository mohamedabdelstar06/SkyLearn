namespace SkyLearnApi.DTOs.Lectures
{
    public class UpdateLectureDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public IFormFile? File { get; set; }
        public List<IFormFile>? AdditionalFiles { get; set; }
    }
}
