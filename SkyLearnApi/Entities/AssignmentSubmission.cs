namespace SkyLearnApi.Entities
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        public int StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
        public int? GradedById { get; set; }
        public ApplicationUser? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }
        public string Status { get; set; } = "Submitted"; // Submitted, Graded, Late, Resubmitted
        public bool IsLate { get; set; } = false;
    }
}
