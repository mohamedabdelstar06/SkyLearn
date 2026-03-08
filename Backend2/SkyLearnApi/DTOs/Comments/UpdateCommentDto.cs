namespace SkyLearnApi.DTOs.Comments
{
    public class UpdateCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
