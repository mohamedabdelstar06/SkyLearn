using SkyLearnApi.DTOs.Activities;

namespace SkyLearnApi.Services.Implementation
{
    public class StudentActivityService : IStudentActivityService
    {
        private readonly AppDbContext _context;
        private readonly IActivityService _activityService;
        private readonly ILogger<StudentActivityService> _logger;

        public StudentActivityService(AppDbContext context, IActivityService activityService, ILogger<StudentActivityService> logger)
        {
            _context = context;
            _activityService = activityService;
            _logger = logger;
        }

        public async Task<PagedStudentActivityResponse> GetMyActivitiesAsync(int studentId, StudentActivityFilterParams filter)
        {
            // Get all courses where student is enrolled
            var studentProfile = await _context.StudentProfiles
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (studentProfile == null)
                return new PagedStudentActivityResponse();

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentProfileId == studentProfile.Id)
                .Select(e => e.CourseId)
                .ToListAsync();

            // Query activities from enrolled courses
            var query = _context.Activities
                .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.IsVisible)
                .Where(a => a.TargetSquadronId == null || a.TargetSquadronId == studentProfile.SquadronId)
                .Include(a => a.Course)
                .AsQueryable();

            // Filter by type
            if (!string.IsNullOrEmpty(filter.ActivityType))
            {
                query = filter.ActivityType switch
                {
                    "Lecture" => query.Where(a => a is Lecture),
                    "Quiz" => query.Where(a => a is Quiz),
                    "Assignment" => query.Where(a => a is Assignment),
                    _ => query
                };
            }

            if (filter.CourseId.HasValue)
                query = query.Where(a => a.CourseId == filter.CourseId.Value);

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(a => a.Title.Contains(filter.Search) || (a.Description != null && a.Description.Contains(filter.Search)));

            // Get progress data
            var totalCount = await query.CountAsync();

            var activities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var activityIds = activities.Select(a => a.Id).ToList();
            var progressMap = await _context.StudentActivityProgress
                .Where(p => activityIds.Contains(p.ActivityId) && p.StudentId == studentId)
                .ToDictionaryAsync(p => p.ActivityId);

            // Filter by status if needed
            var items = activities.Select(a =>
            {
                progressMap.TryGetValue(a.Id, out var progress);
                var activityType = a switch
                {
                    Lecture => "Lecture",
                    Quiz => "Quiz",
                    Assignment => "Assignment",
                    _ => "Unknown"
                };

                return new StudentActivityDto
                {
                    ActivityId = a.Id,
                    CourseId = a.CourseId,
                    CourseTitle = a.Course?.Title ?? "",
                    ActivityType = activityType,
                    Title = a.Title,
                    Description = a.Description,
                    DeadLineDate = a.DeadLineDate,
                    Status = progress?.Status ?? "NotStarted",
                    ProgressPercent = progress?.ProgressPercent ?? 0,
                    IsViewed = progress?.IsViewed ?? false,
                    CreatedAt = a.CreatedAt
                };
            }).ToList();

            if (!string.IsNullOrEmpty(filter.Status))
                items = items.Where(i => i.Status == filter.Status).ToList();

            return new PagedStudentActivityResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        public async Task<PagedCourseGradesDto> GetCourseGradesAsync(int courseId, int userId, string userRole, CourseGradesFilterParams filter)
        {
            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException("Course not found.");

            // Instructors can only see grades for courses they own
            if (userRole == Roles.Instructor)
            {
                if (course.InstructorId != userId)
                    throw new ArgumentException("You are not the instructor of this course.");
            }

            // Fetch all enrolled students for this course
            var enrolledQuery = _context.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.StudentProfile)
                    .ThenInclude(sp => sp.User)
                .AsQueryable();

            // Apply student name search
            if (!string.IsNullOrWhiteSpace(filter.StudentSearch))
            {
                var search = filter.StudentSearch.Trim().ToLower();
                enrolledQuery = enrolledQuery.Where(e =>
                    e.StudentProfile.User.FullName.ToLower().Contains(search) ||
                    e.StudentProfile.UserId.ToString().Contains(search));
            }

            var totalStudents = await enrolledQuery.CountAsync();

            var enrollments = await enrolledQuery
                .OrderBy(e => e.StudentProfile.User.FullName)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var studentIds = enrollments.Select(e => e.StudentProfile.UserId).ToList();

            // ── Lectures ──────────────────────────────────────────────────────────
            var allLectureIds = await _context.Lectures
                .Where(l => l.CourseId == courseId && l.IsVisible)
                .Select(l => l.Id)
                .ToListAsync();

            var totalLectures = allLectureIds.Count;

            // For each student, how many lectures are viewed (Completed)?
            var lectureProgressList = await _context.StudentActivityProgress
                .Where(p => allLectureIds.Contains(p.ActivityId)
                         && studentIds.Contains(p.StudentId)
                         && p.Status == "Completed")
                .GroupBy(p => p.StudentId)
                .Select(g => new { StudentId = g.Key, ViewedCount = g.Count() })
                .ToListAsync();

            var lectureProgressMap = lectureProgressList.ToDictionary(x => x.StudentId, x => x.ViewedCount);

            // ── Quizzes ──────────────────────────────────────────────────────────
            var quizGradesAll = await _context.QuizAttempts
                .Where(a => a.Quiz.CourseId == courseId && studentIds.Contains(a.StudentId))
                .Include(a => a.Quiz)
                .ToListAsync();

            // ── Assignments ───────────────────────────────────────────────────────
            var assignmentGradesAll = await _context.AssignmentSubmissions
                .Where(s => s.Assignment.CourseId == courseId && studentIds.Contains(s.StudentId))
                .Include(s => s.Assignment)
                .ToListAsync();

            // ── Build per-student records ─────────────────────────────────────────
            var studentRecords = new List<StudentCourseGradeDto>();

            foreach (var enrollment in enrollments)
            {
                var sid = enrollment.StudentProfile.UserId;

                // Lecture progress
                var viewedCount = lectureProgressMap.TryGetValue(sid, out var vc) ? vc : 0;
                var lecturePercent = totalLectures > 0
                    ? Math.Round((decimal)viewedCount / totalLectures * 100, 1)
                    : 0m;

                // Quiz grades — best attempt per quiz
                var quizGrades = quizGradesAll
                    .Where(a => a.StudentId == sid)
                    .GroupBy(a => new { a.QuizId, a.Quiz.Title, a.Quiz.TotalMarks })
                    .Select(g => new QuizGradeItem
                    {
                        QuizId = g.Key.QuizId,
                        QuizTitle = g.Key.Title,
                        MaxScore = g.Key.TotalMarks,
                        Score = g.OrderByDescending(a => a.Score).Select(a => a.Score).FirstOrDefault(),
                        ScorePercent = g.OrderByDescending(a => a.Score).Select(a => a.ScorePercent).FirstOrDefault(),
                        Status = g.OrderByDescending(a => a.AttemptNumber).Select(a => a.Status).FirstOrDefault() ?? "NotAttempted",
                        SubmittedAt = g.OrderByDescending(a => a.AttemptNumber).Select(a => a.SubmittedAt).FirstOrDefault()
                    })
                    .ToList();

                // Assignment grades
                var assignmentGrades = assignmentGradesAll
                    .Where(s => s.StudentId == sid)
                    .Select(s => new AssignmentGradeItem
                    {
                        AssignmentId = s.AssignmentId,
                        AssignmentTitle = s.Assignment.Title,
                        Grade = s.Grade,
                        MaxGrade = s.Assignment.MaxGrade,
                        Status = s.Status,
                        SubmittedAt = s.SubmittedAt
                    })
                    .ToList();

                // Apply ItemType filter if requested
                if (!string.IsNullOrWhiteSpace(filter.ItemType))
                {
                    if (filter.ItemType == "Quiz") assignmentGrades.Clear();
                    else if (filter.ItemType == "Assignment") quizGrades.Clear();
                    else if (filter.ItemType == "Lecture") { quizGrades.Clear(); assignmentGrades.Clear(); }
                }

                studentRecords.Add(new StudentCourseGradeDto
                {
                    StudentId = sid,
                    StudentName = enrollment.StudentProfile.User.FullName,
                    TotalLectures = totalLectures,
                    ViewedLectures = viewedCount,
                    LectureProgressPercent = lecturePercent,
                    QuizGrades = quizGrades,
                    AssignmentGrades = assignmentGrades
                });
            }

            return new PagedCourseGradesDto
            {
                CourseId = courseId,
                CourseName = course.Title,
                TotalStudents = totalStudents,
                Page = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalStudents / (double)filter.PageSize),
                Students = studentRecords
            };
        }

        public async Task<GradeRecordDto> GetMyGradesAsync(int courseId, int studentId)
        {
            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException("Course not found.");

            return await BuildMyGradeRecord(courseId, course.Title, studentId);
        }

        private async Task<GradeRecordDto> BuildMyGradeRecord(int courseId, string courseName, int studentId)
        {
            var quizGrades = await _context.QuizAttempts
                .Where(a => a.Quiz.CourseId == courseId && a.StudentId == studentId)
                .Include(a => a.Quiz)
                .GroupBy(a => new { a.QuizId, a.Quiz.Title, a.Quiz.TotalMarks })
                .Select(g => new QuizGradeItem
                {
                    QuizId = g.Key.QuizId,
                    QuizTitle = g.Key.Title,
                    MaxScore = g.Key.TotalMarks,
                    Score = g.OrderByDescending(a => a.Score).Select(a => a.Score).FirstOrDefault(),
                    ScorePercent = g.OrderByDescending(a => a.Score).Select(a => a.ScorePercent).FirstOrDefault(),
                    Status = g.OrderByDescending(a => a.AttemptNumber).Select(a => a.Status).FirstOrDefault() ?? "NotAttempted",
                    SubmittedAt = g.OrderByDescending(a => a.AttemptNumber).Select(a => a.SubmittedAt).FirstOrDefault()
                })
                .ToListAsync();

            var assignmentGrades = await _context.AssignmentSubmissions
                .Where(s => s.Assignment.CourseId == courseId && s.StudentId == studentId)
                .Include(s => s.Assignment)
                .Select(s => new AssignmentGradeItem
                {
                    AssignmentId = s.AssignmentId,
                    AssignmentTitle = s.Assignment.Title,
                    Grade = s.Grade,
                    MaxGrade = s.Assignment.MaxGrade,
                    Status = s.Status,
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();

            return new GradeRecordDto
            {
                CourseId = courseId,
                CourseName = courseName,
                QuizGrades = quizGrades,
                AssignmentGrades = assignmentGrades
            };
        }
    }
}
