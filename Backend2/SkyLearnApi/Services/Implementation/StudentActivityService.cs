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
                    DueDate = a.DueDate,
                    Status = progress?.Status ?? "NotStarted",
                    ProgressPercent = progress?.ProgressPercent ?? 0,
                    TotalTimeSpentSeconds = progress?.TotalTimeSpentSeconds ?? 0,
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

        public async Task StartSessionAsync(int activityId, int studentId)
        {
            var progress = await _context.StudentActivityProgress
                .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.StudentId == studentId);

            if (progress == null)
            {
                progress = new StudentActivityProgress
                {
                    ActivityId = activityId,
                    StudentId = studentId,
                    Status = "InProgress",
                    FirstAccessedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };
                _context.StudentActivityProgress.Add(progress);
            }
            else
            {
                if (progress.FirstAccessedAt == null)
                    progress.FirstAccessedAt = DateTime.UtcNow;
                progress.LastAccessedAt = DateTime.UtcNow;
                progress.Status = progress.Status == "Completed" ? "Completed" : "InProgress";
            }

            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.ActivityStarted, "Activity", activityId, studentId,
                "Student started activity session");
        }

        public async Task HeartbeatAsync(int activityId, int studentId, HeartbeatDto dto)
        {
            var progress = await _context.StudentActivityProgress
                .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.StudentId == studentId);

            if (progress == null) return;

            var now = DateTime.UtcNow;
            if (progress.LastAccessedAt.HasValue)
            {
                var elapsed = (long)(now - progress.LastAccessedAt.Value).TotalSeconds;
                // Clamp to 120 seconds to prevent stale sessions from inflating time
                if (elapsed > 0 && elapsed <= 120)
                    progress.TotalTimeSpentSeconds += elapsed;
            }

            progress.LastAccessedAt = now;
            progress.ProgressPercent = Math.Min(dto.ProgressPercent, 100);

            if (dto.ProgressPercent >= 100 && progress.Status != "Completed")
            {
                progress.Status = "Completed";
                progress.CompletedAt = now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task EndSessionAsync(int activityId, int studentId)
        {
            var progress = await _context.StudentActivityProgress
                .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.StudentId == studentId);

            if (progress == null) return;

            var now = DateTime.UtcNow;
            if (progress.LastAccessedAt.HasValue)
            {
                var elapsed = (long)(now - progress.LastAccessedAt.Value).TotalSeconds;
                if (elapsed > 0 && elapsed <= 120)
                    progress.TotalTimeSpentSeconds += elapsed;
            }

            progress.LastAccessedAt = now;
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.ActivityEnded, "Activity", activityId, studentId,
                $"Session ended. Total time: {progress.TotalTimeSpentSeconds}s, Progress: {progress.ProgressPercent}%",
                metadata: new { timeSpentSeconds = progress.TotalTimeSpentSeconds, progressPercent = progress.ProgressPercent });
        }

        public async Task<GradeRecordDto> GetCourseGradesAsync(int courseId, int userId, string userRole)
        {
            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException("Course not found.");

            return await BuildGradeRecord(courseId, course.Title, null);
        }

        public async Task<GradeRecordDto> GetMyGradesAsync(int courseId, int studentId)
        {
            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException("Course not found.");

            return await BuildGradeRecord(courseId, course.Title, studentId);
        }

        private async Task<GradeRecordDto> BuildGradeRecord(int courseId, string courseName, int? studentId)
        {
            var quizGrades = await _context.QuizAttempts
                .Where(a => a.Quiz.CourseId == courseId && (studentId == null || a.StudentId == studentId))
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
                .Where(s => s.Assignment.CourseId == courseId && (studentId == null || s.StudentId == studentId))
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
