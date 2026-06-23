namespace SkyLearnApi.Entities
{
    public class Lecture : Activity
    {
        public string? ContentType { get; set; } // Video, Pdf, Audio, Mixed
        public string? FileUrl { get; set; }
        public string? AdditionalFileUrls { get; set; } // JSON array for Mixed
        public string? ThumbnailUrl { get; set; }
        public string? Transcript { get; set; }
        public string? AiSummary { get; set; }
        public DateTime? TranscriptGeneratedAt { get; set; }
        public DateTime? SummaryGeneratedAt { get; set; }
        
        // Background Job Tracking
        public string SummaryStatus { get; set; } = "None"; // None, Pending, Completed, Failed
        public int? EstimatedCompletionMinutes { get; set; }
    }
}
