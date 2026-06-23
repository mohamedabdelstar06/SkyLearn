namespace SkyLearnApi.DTOs.Comments
{
    public class CreateCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
    }
}
