namespace SkyLearnApi.Services.Implementations
{
    public class CourseService : ICourseService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseService(AppDbContext context, IMapper mapper, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _mapper = mapper;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IEnumerable<CourseResponseDto>> GetAllAsync(
            string? search, int? departmentId, int? yearId,
            DateTime? startDate, DateTime? endDate,
            int page = 1, int pageSize = 9, int? userId = null, string? userRole = null)
        {
            var query = _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Year)
                .Include(c => c.Instructor)
                .AsQueryable();
            // Instructors can only see courses where they are the assigned instructor
            if (userRole == Roles.Instructor && userId.HasValue)
            {
                query = query.Where(c => c.InstructorId == userId.Value);
            }

            if (departmentId.HasValue)
                query = query.Where(c => c.DepartmentId == departmentId.Value);

            if (yearId.HasValue)
                query = query.Where(c => c.YearId == yearId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Title.Contains(search) ||
                    (c.Description != null && c.Description.Contains(search)));
            }

            if (startDate.HasValue)
                query = query.Where(c => c.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.CreatedAt <= endDate.Value);

            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get enrolled student counts for these courses
            var courseIds = courses.Select(c => c.Id).ToList();
            var yearIds = courses.Select(c => c.YearId).Distinct().ToList();

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

            return courses.Select(c => new CourseResponseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department.Name,
                YearId = c.YearId,
                YearName = c.Year.Name,
                CreditHours = c.CreditHours,
                EnrolledStudentsCount = autoEnrollmentCounts.GetValueOrDefault(c.YearId, 0) + manualEnrollmentCounts.GetValueOrDefault(c.Id, 0),
                ImageUrl = c.ImageUrl,
                InstructorId = c.InstructorId,
                InstructorName = c.Instructor.FullName,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });
        }

        public async Task<CourseResponseDto?> GetByIdAsync(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Year)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return null;

            // Count includes: (1) students automatically enrolled via their academic year
            //                 (2) students manually enrolled via Enrollment endpoint from other years
            var autoEnrolledCount = await _context.StudentProfiles.CountAsync(sp => sp.YearId == course.YearId);
            var manualEnrolledCount = await _context.Enrollments
                .Include(e => e.StudentProfile)
                .CountAsync(e => e.CourseId == id && e.StudentProfile.YearId != course.YearId);
            var enrolledCount = autoEnrolledCount + manualEnrolledCount;

            return new CourseResponseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                DepartmentId = course.DepartmentId,
                DepartmentName = course.Department.Name,
                YearId = course.YearId,
                YearName = course.Year.Name,
                CreditHours = course.CreditHours,
                EnrolledStudentsCount = enrolledCount,
                ImageUrl = course.ImageUrl,
                InstructorId = course.InstructorId,
                InstructorName = course.Instructor.FullName,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt
            };
        }

        public async Task<CourseResponseDto> CreateAsync(CourseRequestDto dto, int userId)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == dto.DepartmentName);
            if (department == null)
                throw new ArgumentException($"Department '{dto.DepartmentName}' not found.");

            var year = await _context.Years
                .FirstOrDefaultAsync(y => y.Name == dto.YearName && y.DepartmentId == department.Id);
            if (year == null)
                throw new ArgumentException($"Year '{dto.YearName}' not found in department '{dto.DepartmentName}'.");

            // Determine the instructor for this course
            int instructorId;
            if (dto.InstructorId.HasValue)
            {
                // InstructorId was provided - validate the user exists and is Admin or Instructor
                var assignedUser = await _userManager.FindByIdAsync(dto.InstructorId.Value.ToString());
                if (assignedUser == null)
                    throw new ArgumentException($"User with ID {dto.InstructorId.Value} not found.");

                var userRoles = await _userManager.GetRolesAsync(assignedUser);
                if (!userRoles.Contains(Roles.Instructor) && !userRoles.Contains(Roles.Admin))
                    throw new ArgumentException($"User with ID {dto.InstructorId.Value} must be an Admin or Instructor.");

                instructorId = dto.InstructorId.Value;
            }
            else
            {
                // No InstructorId specified - the creating user becomes the instructor
                instructorId = userId;
            }

            var course = _mapper.Map<Course>(dto);
            course.DepartmentId = department.Id;
            course.YearId = year.Id;
            course.InstructorId = instructorId;
            course.CreatedById = userId;

            if (dto.ImageFile != null)
            {
                if (string.IsNullOrEmpty(_env.WebRootPath))
                    throw new InvalidOperationException("WebRootPath is not configured.");

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "courses");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{dto.ImageFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.ImageFile.CopyToAsync(stream);

                course.ImageUrl = $"/uploads/courses/{fileName}";
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            await UpdateYearTotalsAsync(course.YearId);

            var instructor = await _context.Users.FindAsync(instructorId);
            
            return new CourseResponseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                DepartmentId = department.Id,
                DepartmentName = department.Name,
                YearId = year.Id,
                YearName = year.Name,
                CreditHours = course.CreditHours,
                EnrolledStudentsCount = 0,
                ImageUrl = course.ImageUrl,
                InstructorId = instructorId,
                InstructorName = instructor?.FullName ?? "",
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt
            };
        }

        public async Task<CourseResponseDto?> UpdateAsync(int id, CourseRequestDto dto, int userId)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return null;

            // Admins can update any course, Instructors can only update courses they are assigned to
            var userRole = await GetUserRoleAsync(userId);
            if (userRole != Roles.Admin && course.InstructorId != userId)
                throw new UnauthorizedAccessException("You are not allowed to update this course.");

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == dto.DepartmentName);
            if (department == null)
                throw new ArgumentException($"Department '{dto.DepartmentName}' not found.");

            var year = await _context.Years
                .FirstOrDefaultAsync(y => y.Name == dto.YearName && y.DepartmentId == department.Id);
            if (year == null)
                throw new ArgumentException($"Year '{dto.YearName}' not found in department '{dto.DepartmentName}'.");
            
            var oldYearId = course.YearId;

            _mapper.Map(dto, course);
            
            course.DepartmentId = department.Id;
            course.YearId = year.Id;
            course.UpdatedAt = DateTime.UtcNow;

            // Admin can reassign the instructor (new instructor can be Admin or Instructor)
            if (userRole == Roles.Admin && dto.InstructorId.HasValue && dto.InstructorId.Value != course.InstructorId)
            {
                var newUser = await _userManager.FindByIdAsync(dto.InstructorId.Value.ToString());
                if (newUser == null)
                    throw new ArgumentException($"User with ID {dto.InstructorId.Value} not found.");

                var newUserRoles = await _userManager.GetRolesAsync(newUser);
                if (!newUserRoles.Contains(Roles.Instructor) && !newUserRoles.Contains(Roles.Admin))
                    throw new ArgumentException($"User with ID {dto.InstructorId.Value} must be an Admin or Instructor.");

                course.InstructorId = dto.InstructorId.Value;
            }

            if (dto.ImageFile != null)
            {
                if (string.IsNullOrEmpty(_env.WebRootPath))
                    throw new InvalidOperationException("WebRootPath is not configured.");

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "courses");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{dto.ImageFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.ImageFile.CopyToAsync(stream);

                course.ImageUrl = $"/uploads/courses/{fileName}";
            }

            await _context.SaveChangesAsync();

            await UpdateYearTotalsAsync(course.YearId);
            if (oldYearId != course.YearId)
                await UpdateYearTotalsAsync(oldYearId);

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return false;

            // Admins can delete any course, Instructors can only delete courses they are assigned to
            var userRole = await GetUserRoleAsync(userId);
            if (userRole != Roles.Admin && course.InstructorId != userId)
                throw new UnauthorizedAccessException("You are not allowed to delete this course.");

            var yearId = course.YearId;

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await UpdateYearTotalsAsync(yearId);

            return true;
        }

        private async Task UpdateYearTotalsAsync(int yearId)
        {
            var totalCourses = await _context.Courses.CountAsync(c => c.YearId == yearId);
            var totalHours = await _context.Courses
                .Where(c => c.YearId == yearId)
                .Select(c => (int?)c.CreditHours)
                .SumAsync() ?? 0;

            var year = await _context.Years.FindAsync(yearId);
            if (year != null)
            {
                year.TotalCourses = totalCourses;
                year.TotalHours = totalHours;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<string?> GetUserRoleAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            // Return the first role (users typically have one role in this system)
            return roles.FirstOrDefault();
        }
    }
}
