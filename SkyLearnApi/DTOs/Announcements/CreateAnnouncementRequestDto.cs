using SkyLearnApi.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SkyLearnApi.DTOs.Announcements
{
    public class CreateAnnouncementRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(2000)]
        public string? ImageUrl { get; set; }

        public Microsoft.AspNetCore.Http.IFormFile? ImageFile { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsPinned { get; set; }

        public AnnouncementAudienceType AudienceType { get; set; }

        public int? DepartmentId { get; set; }
        public int? YearId { get; set; }
        public int? SquadronId { get; set; }
        public int? CourseId { get; set; }
    }
}
