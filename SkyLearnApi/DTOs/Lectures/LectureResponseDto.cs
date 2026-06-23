namespace SkyLearnApi.DTOs.Lectures
{
    public class LectureResponseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? AdditionalFileUrls { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool HasSummary { get; set; }
        public bool HasTranscript { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CommentCount { get; set; }

        // Student-specific: populated from StudentActivityProgress
        public bool? IsViewed { get; set; }
        public DateTime? ViewedAt { get; set; }
    }
}
