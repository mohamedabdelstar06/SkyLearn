using SkyLearnApi.Enums;
using System;

namespace SkyLearnApi.Entities
{
    public class Announcement
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        public int CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsPinned { get; set; }

        public AnnouncementAudienceType AudienceType { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? YearId { get; set; }
        public Year? Year { get; set; }

        public int? SquadronId { get; set; }
        public Squadron? Squadron { get; set; }

        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
