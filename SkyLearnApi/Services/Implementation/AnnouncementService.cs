using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkyLearnApi.Data;
using SkyLearnApi.DTOs.Announcements;
using SkyLearnApi.DTOs.Common;
using SkyLearnApi.Entities;
using SkyLearnApi.Enums;
using SkyLearnApi.Services.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyLearnApi.Services.Implementation
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _environment;

        public AnnouncementService(AppDbContext context, UserManager<ApplicationUser> userManager, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<AnnouncementDetailsDto> CreateAnnouncementAsync(int userId, CreateAnnouncementRequestDto request, CancellationToken cancellationToken = default)
        {
            ValidateAnnouncementRules(request);

            var announcement = request.Adapt<Announcement>();
            announcement.CreatedByUserId = userId;
            announcement.CreatedAt = DateTime.UtcNow;

            if (request.ImageFile != null)
            {
                announcement.ImageUrl = await SkyLearnApi.Helpers.ImageHelper.SaveImageAsync(request.ImageFile, "Announcements", _environment);
            }

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync(cancellationToken);

            return await GetAnnouncementByIdAsync(announcement.Id, cancellationToken);
        }

        public async Task<AnnouncementDetailsDto> UpdateAnnouncementAsync(int id, UpdateAnnouncementRequestDto request, CancellationToken cancellationToken = default)
        {
            ValidateAnnouncementRules(request);

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            if (announcement == null)
            {
                throw new Exception("Announcement not found");
            }

            request.Adapt(announcement);

            if (request.ImageFile != null)
            {
                announcement.ImageUrl = await SkyLearnApi.Helpers.ImageHelper.SaveImageAsync(request.ImageFile, "Announcements", _environment);
            }

            announcement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return await GetAnnouncementByIdAsync(announcement.Id, cancellationToken);
        }

        public async Task DeleteAnnouncementAsync(int id, CancellationToken cancellationToken = default)
        {
            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

            if (announcement == null)
            {
                throw new Exception("Announcement not found");
            }

            announcement.IsDeleted = true;
            announcement.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<AnnouncementDetailsDto> GetAnnouncementByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var announcement = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .Include(a => a.Department)
                .Include(a => a.Year)
                .Include(a => a.Squadron)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (announcement == null)
            {
                throw new Exception("Announcement not found");
            }

            var dto = announcement.Adapt<AnnouncementDetailsDto>();
            dto.CreatedByUserName = announcement.CreatedByUser?.FullName ?? "Unknown";
            dto.DepartmentName = announcement.Department?.Name;
            dto.YearName = announcement.Year?.Name;
            dto.SquadronName = announcement.Squadron?.Name;
            dto.CourseName = announcement.Course?.Title;

            return dto;
        }

        public async Task<PaginatedList<AnnouncementListDto>> GetAllAnnouncementsAdminAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Announcements.AsNoTracking()
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectToType<AnnouncementListDto>()
                .ToListAsync(cancellationToken);

            return new PaginatedList<AnnouncementListDto>(items, totalCount, page, pageSize);
        }

        public async Task<PaginatedList<AnnouncementListDto>> GetAllAnnouncementsForUserAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = await BuildUserAudienceQueryAsync(userId, false);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectToType<AnnouncementListDto>()
                .ToListAsync(cancellationToken);

            return new PaginatedList<AnnouncementListDto>(items, totalCount, page, pageSize);
        }

        public async Task<PaginatedList<AnnouncementListDto>> GetActiveAnnouncementsForUserAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = await BuildUserAudienceQueryAsync(userId, true);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectToType<AnnouncementListDto>()
                .ToListAsync(cancellationToken);

            return new PaginatedList<AnnouncementListDto>(items, totalCount, page, pageSize);
        }

        private async Task<IQueryable<Announcement>> BuildUserAudienceQueryAsync(int userId, bool activeOnly)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new Exception("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");
            bool isInstructor = roles.Contains("Instructor");
            bool isStudent = roles.Contains("Student");

            var query = _context.Announcements.AsNoTracking().Where(a => !a.IsDeleted);

            if (activeOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(a => a.StartDate <= now && a.EndDate >= now);
            }

            if (isAdmin)
            {
                // Admin user sees All and Admins announcements
                return query.Where(a => a.AudienceType == AnnouncementAudienceType.All || a.AudienceType == AnnouncementAudienceType.Admins);
            }
            else if (isInstructor)
            {
                // Instructor sees All and Instructors announcements
                return query.Where(a => a.AudienceType == AnnouncementAudienceType.All || a.AudienceType == AnnouncementAudienceType.Instructors);
            }
            else if (isStudent)
            {
                var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentProfileId == profile.Id)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                return query.Where(a => 
                    a.AudienceType == AnnouncementAudienceType.All || 
                    a.AudienceType == AnnouncementAudienceType.Students ||
                    (a.AudienceType == AnnouncementAudienceType.Department && a.DepartmentId == profile.DepartmentId) ||
                    (a.AudienceType == AnnouncementAudienceType.AcademicYear && a.YearId == profile.YearId) ||
                    (a.AudienceType == AnnouncementAudienceType.Squadron && a.SquadronId == profile.SquadronId) ||
                    (a.AudienceType == AnnouncementAudienceType.Course && a.CourseId != null && enrolledCourseIds.Contains(a.CourseId.Value))
                );
            }

            // Fallback for no role
            return query.Where(a => a.AudienceType == AnnouncementAudienceType.All);
        }

        private void ValidateAnnouncementRules(dynamic request)
        {
            if (request.EndDate < request.StartDate)
            {
                throw new Exception("EndDate must be greater than or equal to StartDate.");
            }

            switch ((AnnouncementAudienceType)request.AudienceType)
            {
                case AnnouncementAudienceType.Department:
                    if (request.DepartmentId == null) throw new Exception("DepartmentId is required for Department audience.");
                    break;
                case AnnouncementAudienceType.AcademicYear:
                    if (request.YearId == null) throw new Exception("YearId is required for AcademicYear audience.");
                    break;
                case AnnouncementAudienceType.Squadron:
                    if (request.SquadronId == null) throw new Exception("SquadronId is required for Squadron audience.");
                    break;
                case AnnouncementAudienceType.Course:
                    if (request.CourseId == null) throw new Exception("CourseId is required for Course audience.");
                    break;
                case AnnouncementAudienceType.All:
                case AnnouncementAudienceType.Students:
                case AnnouncementAudienceType.Instructors:
                case AnnouncementAudienceType.Admins:
                    request.DepartmentId = null;
                    request.YearId = null;
                    request.SquadronId = null;
                    request.CourseId = null;
                    break;
            }
        }
    }
}
