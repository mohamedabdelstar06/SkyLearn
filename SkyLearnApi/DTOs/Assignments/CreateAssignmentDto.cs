namespace SkyLearnApi.DTOs.Assignments
{
    public class CreateAssignmentDto : IValidatableObject
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Instructions { get; set; }
        public decimal MaxGrade { get; set; } = 100;
        public bool AllowLateSubmission { get; set; } = false;
        
        [Required(ErrorMessage = "StartDate is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "DeadLineDate is required.")]
        public DateTime DeadLineDate { get; set; }
        
        public int? TargetSquadronId { get; set; }
        public bool IsVisible { get; set; } = true;
        public List<IFormFile>? AssignmentFiles { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DeadLineDate <= StartDate)
            {
                yield return new ValidationResult("DeadLineDate must be greater than StartDate.", new[] { nameof(DeadLineDate) });
            }
        }
    }
}
