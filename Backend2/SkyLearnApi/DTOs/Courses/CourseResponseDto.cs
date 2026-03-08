using System.Text.Json.Serialization;

namespace SkyLearnApi.Dtos.Courses
{
    public class CourseResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;

        public int YearId { get; set; }
        public string YearName { get; set; } = string.Empty;

        public int CreditHours { get; set; }
        public int EnrolledStudentsCount { get; set; } = 0;

        public string? ImageUrl { get; set; }

        public int InstructorId { get; set; }
        public string InstructorName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Activity counts
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? LecturesCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? QuizzesCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AssignmentsCount { get; set; }

        // Student-specific progress (only populated when a student requests)
        public decimal? ProgressPercentage { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        public InstructorInfoDto? Instructor { get; set; }
    }

    public class InstructorInfoDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsActivated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
