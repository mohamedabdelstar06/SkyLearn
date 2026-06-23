using SkyLearnApi.Enums;
using System;

namespace SkyLearnApi.DTOs.Announcements
{
    public class AnnouncementDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPinned { get; set; }
        public AnnouncementAudienceType AudienceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;

        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        public int? YearId { get; set; }
        public string? YearName { get; set; }

        public int? SquadronId { get; set; }
        public string? SquadronName { get; set; }

        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
    }
}
