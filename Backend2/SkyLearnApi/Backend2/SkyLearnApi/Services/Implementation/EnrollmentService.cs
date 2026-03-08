using SkyLearnApi.DTOs.Enrollment;

namespace SkyLearnApi.Services.Implementation
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly AppDbContext _context;

        public EnrollmentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentCourseDto>> GetStudentCoursesAsync(int studentId)
        {
            var profile = await _context.StudentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (profile == null)
            {
                Log.Warning("GetStudentCourses: No profile found for user {UserId}", studentId);
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
                return new List<StudentCourseDto>();

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
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (profile == null)
            {
                Log.Warning("Enroll failed: Student {StudentId} not found or has no profile", studentId);
                return (false, "Student not found or is not a student.");
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                Log.Warning("Enroll failed: Course {CourseId} not found", courseId);
                return (false, "Course not found.");
            }
            // Instructors can only enroll students in courses they are assigned to
            if (userRole == Roles.Instructor && course.InstructorId != enrolledById)
            {
                Log.Warning("Enroll failed: Instructor {InstructorId} attempted to enroll student in course {CourseId} they are not assigned to", enrolledById, courseId);
                return (false, "You can only enroll students in courses you are assigned to.");
            }

            var exists = await _context.Enrollments
                .AnyAsync(e => e.StudentProfileId == profile.Id && e.CourseId == courseId);

            if (exists)
            {
                Log.Warning("Enroll failed: Student {StudentId} already enrolled in course {CourseId}", studentId, courseId);
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

            Log.Information("Student {StudentId} enrolled in course {CourseId} by user {EnrolledBy}",
                studentId, courseId, enrolledById);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UnenrollStudentAsync(int studentId, int courseId, int userId, string userRole)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (profile == null)
            {
                Log.Warning("Unenroll failed: Student {StudentId} not found", studentId);
                return (false, "Student not found.");
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                Log.Warning("Unenroll failed: Course {CourseId} not found", courseId);
                return (false, "Course not found.");
            }

            // Instructors can only unenroll students from courses they are assigned to
            if (userRole == Roles.Instructor && course.InstructorId != userId)
            {
                Log.Warning("Unenroll failed: Instructor {InstructorId} attempted to unenroll student from course {CourseId} they are not assigned to", userId, courseId);
                return (false, "You can only unenroll students from courses you are assigned to.");
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentProfileId == profile.Id && e.CourseId == courseId);

            if (enrollment == null)
            {
                Log.Warning("Unenroll failed: Student {StudentId} not enrolled in course {CourseId}", studentId, courseId);
                return (false, "Student is not enrolled in this course.");
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            Log.Information("Student {StudentId} unenrolled from course {CourseId} by user {UserId}", studentId, courseId, userId);

            return (true, null);
        }
    }
}
