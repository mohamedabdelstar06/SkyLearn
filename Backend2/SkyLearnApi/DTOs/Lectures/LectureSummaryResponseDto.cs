namespace SkyLearnApi.DTOs.Lectures
{
    public class LectureSummaryResponseDto
    {
        public int LectureId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AiSummary { get; set; }
        public string? Transcript { get; set; }
        public DateTime? SummaryGeneratedAt { get; set; }
        public DateTime? TranscriptGeneratedAt { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public int? EstimatedCompletionMinutes { get; set; }
    }
}
