namespace SkyLearnApi.Entities
{
    public class Assignment : Activity
    {
        public decimal MaxGrade { get; set; } = 100;
        public bool AllowLateSubmission { get; set; } = false;
        public string? Instructions { get; set; }
        public string? AssignmentFileUrls { get; set; } 
        public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }
}
