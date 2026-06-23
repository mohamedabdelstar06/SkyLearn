namespace SkyLearnApi.DTOs.Comments
{
    public class CommentResponseDto
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string? UserProfileImageUrl { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsOwner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = new();
    }
}
