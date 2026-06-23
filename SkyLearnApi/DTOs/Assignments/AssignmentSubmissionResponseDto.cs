namespace SkyLearnApi.DTOs.Assignments
{
    public class AssignmentSubmissionResponseDto
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime SubmittedAt { get; set; }
        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsLate { get; set; }
        public int? GradedById { get; set; }
        public string? GradedByName { get; set; }
        public DateTime? GradedAt { get; set; }
    }
}
