using SkyLearnApi.DTOs.Enrollment;

namespace SkyLearnApi.Services.Implementation
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(AppDbContext context, INotificationService notificationService, ILogger<EnrollmentService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<StudentCourseDto>> GetStudentCoursesAsync(int studentId)
        {
            _logger.LogDebug("Fetching courses for student {StudentId}", studentId);

            var profile = await _context.StudentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (profile == null)
            {
                _logger.LogWarning("GetStudentCourses: No profile found for user {UserId}", studentId);
                return new List<StudentCourseDto>();
            }

            // Get courses for this student's year and department (automatic enrollment)
            var yearCourses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.CreatedBy)
                .Where(c => c.YearId == profile.YearId && c.DepartmentId == profile.DepartmentId)
                .ToListAsync();

            // Get courses from other years that this student is manually enrolled in
            var manualEnrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                    .ThenInclude(c => c.CreatedBy)
                .Where(e => e.StudentProfileId == profile.Id && e.Course.YearId != profile.YearId)
                .ToListAsync();

            var manualCourses = manualEnrollments.Select(e => e.Course).ToList();

            // Combine all courses (year courses + manually enrolled courses from other years)
            var allCourses = yearCourses.Concat(manualCourses).DistinctBy(c => c.Id).ToList();

            if (!allCourses.Any())
            {
                _logger.LogDebug("No courses found for student {StudentId} (Year: {YearId}, Dept: {DeptId})",
                    studentId, profile.YearId, profile.DepartmentId);
                return new List<StudentCourseDto>();
            }

            // Get enrolled student counts for these courses
            var courseIds = allCourses.Select(c => c.Id).ToList();
            var yearIds = allCourses.Select(c => c.YearId).Distinct().ToList();

            // Count students automatically enrolled by year
            var autoEnrollmentCounts = await _context.StudentProfiles
                .Where(sp => yearIds.Contains(sp.YearId))
                .GroupBy(sp => sp.YearId)
                .Select(g => new { YearId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.YearId, x => x.Count);
            // Count manually enrolled students from other years (explicit enrollments)
            var manualEnrollmentCounts = await _context.Enrollments
                .Include(e => e.StudentProfile)
                .Where(e => courseIds.Contains(e.CourseId))
                .Where(e => e.StudentProfile.YearId != e.Course.YearId)
                .GroupBy(e => e.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CourseId, x => x.Count);

            // Get this student's manual enrollments (for EnrolledAt on manually enrolled courses)
            var studentManualEnrollmentDates = manualEnrollments.ToDictionary(e => e.CourseId, e => e.EnrolledAt);

            _logger.LogDebug("Returning {Count} courses for student {StudentId} ({AutoCount} auto, {ManualCount} manual)",
                allCourses.Count, studentId, yearCourses.Count, manualCourses.Count);

            return allCourses.Select(c => new StudentCourseDto
            {
                CourseId = c.Id,
                CourseTitle = c.Title,
                CourseDescription = c.Description,
                ImageUrl = c.ImageUrl,
                CreditHours = c.CreditHours,
                EnrolledStudentsCount = autoEnrollmentCounts.GetValueOrDefault(c.YearId, 0) + manualEnrollmentCounts.GetValueOrDefault(c.Id, 0),
                InstructorName = c.CreatedBy.FullName,
                // EnrolledAt: Use manual enrollment date if manually enrolled, otherwise use profile creation date (auto-enrollment)
                EnrolledAt = studentManualEnrollmentDates.TryGetValue(c.Id, out var enrolledAt) 
                    ? enrolledAt 
                    : (c.YearId == profile.YearId ? profile.CreatedAt : (DateTime?)null)
            }).ToList();
        }

        public async Task<(bool Success, string? Error)> EnrollStudentAsync(int studentId, int courseId, int enrolledById, string userRole)
        {
            _logger.LogInformation("Enrolling student {StudentId} in course {CourseId} by user {EnrolledBy} (Role: {Role})",
                studentId, courseId, enrolledById, userRole);

            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (profile == null)
            {
                _logger.LogWarning("Enroll failed: Student {StudentId} not found or has no profile", studentId);
                return (false, "Student not found or is not a student.");
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("Enroll failed: Course {CourseId} not found", courseId);
                return (false, "Course not found.");
            }
            // Instructors can only enroll students in courses they are assigned to
            if (userRole == Roles.Instructor && course.InstructorId != enrolledById)
            {
                _logger.LogWarning("Enroll failed: Instructor {InstructorId} attempted to enroll student in course {CourseId} ('{CourseTitle}') they are not assigned to",
                    enrolledById, courseId, course.Title);
                return (false, "You can only enroll students in courses you are assigned to.");
            }

            var exists = await _context.Enrollments
                .AnyAsync(e => e.StudentProfileId == profile.Id && e.CourseId == courseId);

            if (exists)
            {
                _logger.LogWarning("Enroll failed: Student {StudentId} already enrolled in course {CourseId} ('{CourseTitle}')",
                    studentId, courseId, course.Title);
                return (false, "Student is already enrolled in this course.");
            }

            var enrollment = new Enrollment
            {
                StudentProfileId = profile.Id,
                CourseId = courseId,
                EnrolledById = enrolledById,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student {StudentId} enrolled successfully in course {CourseId} ('{CourseTitle}') by user {EnrolledBy}",
                studentId, courseId, course.Title, enrolledById);

            // Notify the student they were enrolled
            await _notificationService.CreateNotificationAsync(studentId,
                "You've Been Enrolled",
                $"You have been enrolled in course '{course.Title}'.",
                "Enrollment", null);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UnenrollStudentAsync(int studentId, int courseId, int userId, string userRole)
        {
            _logger.LogInformation("Unenrolling student {StudentId} from course {CourseId} by user {UserId} (Role: {Role})",
                studentId, courseId, userId, userRole);

            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (profile == null)
            {
                _logger.LogWarning("Unenroll failed: Student {StudentId} not found", studentId);
                return (false, "Student not found.");
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("Unenroll failed: Course {CourseId} not found", courseId);
                return (false, "Course not found.");
            }

            // Instructors can only unenroll students from courses they are assigned to
            if (userRole == Roles.Instructor && course.InstructorId != userId)
            {
                _logger.LogWarning("Unenroll failed: Instructor {InstructorId} attempted to unenroll student from course {CourseId} ('{CourseTitle}') they are not assigned to",
                    userId, courseId, course.Title);
                return (false, "You can only unenroll students from courses you are assigned to.");
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentProfileId == profile.Id && e.CourseId == courseId);

            if (enrollment == null)
            {
                _logger.LogWarning("Unenroll failed: Student {StudentId} not enrolled in course {CourseId}", studentId, courseId);
                return (false, "Student is not enrolled in this course.");
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student {StudentId} unenrolled from course {CourseId} ('{CourseTitle}') by user {UserId}",
                studentId, courseId, course.Title, userId);

            // Notify the student they were unenrolled
            await _notificationService.CreateNotificationAsync(studentId,
                "Course Enrollment Removed",
                $"You have been removed from course '{course.Title}'.",
                "Unenrollment", null);

            return (true, null);
        }
    }
}

