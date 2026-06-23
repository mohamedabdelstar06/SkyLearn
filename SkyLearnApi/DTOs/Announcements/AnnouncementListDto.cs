using SkyLearnApi.Enums;
using System;

namespace SkyLearnApi.DTOs.Announcements
{
    public class AnnouncementListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPinned { get; set; }
        public AnnouncementAudienceType AudienceType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
