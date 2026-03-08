namespace SkyLearnApi.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public Activity Activity { get; set; } = null!;
        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public int LikeCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
    }
}
