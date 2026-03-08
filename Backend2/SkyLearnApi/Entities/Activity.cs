namespace SkyLearnApi.Entities
{
    public class Activity
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public DateTime? DueDate { get; set; }
        public int? TargetSquadronId { get; set; }
        public Squadron? TargetSquadron { get; set; }
        public int CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        // Navigation properties
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<StudentActivityProgress> StudentProgress { get; set; } = new List<StudentActivityProgress>();
    }
}
