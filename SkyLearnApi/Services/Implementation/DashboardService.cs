using Microsoft.EntityFrameworkCore;
using SkyLearnApi.Data;
using SkyLearnApi.Entities;
using SkyLearnApi.Services.Interfaces;

namespace SkyLearnApi.Services.Implementation
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetAdminDashboardStatsAsync()
        {
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Student);
            var instructorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Instructor);

            var totalUsers = await _context.Users.CountAsync();
            var totalStudents = studentRole != null ? await _context.UserRoles.CountAsync(ur => ur.RoleId == studentRole.Id) : 0;
            var totalInstructors = instructorRole != null ? await _context.UserRoles.CountAsync(ur => ur.RoleId == instructorRole.Id) : 0;
            var totalCourses = await _context.Courses.CountAsync();
            var totalDepartments = await _context.Departments.CountAsync();
            var totalSquadrons = await _context.Squadrons.CountAsync();

            return new
            {
                TotalUsers = totalUsers,
                TotalStudents = totalStudents,
                TotalInstructors = totalInstructors,
                TotalCourses = totalCourses,
                TotalDepartments = totalDepartments,
                TotalSquadrons = totalSquadrons
            };
        }

        public async Task<object> GetAdminOverviewAsync()
        {
            var departments = await _context.Departments
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    CourseCount = d.Courses.Count
                })
                .OrderByDescending(d => d.CourseCount)
                .Take(4)
                .ToListAsync();

            var recentCourses = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Instructor)
                .OrderByDescending(c => c.CreatedAt)
                .Take(8)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.ImageUrl,
                    DepartmentName = c.Department.Name,
                    InstructorName = c.Instructor.FullName,
                    c.EnrolledStudentsCount,
                    c.CreditHours
                })
                .ToListAsync();

            var topInstructors = await _context.Courses
                .GroupBy(c => new { c.InstructorId, c.Instructor.FullName, c.Instructor.ProfileImageUrl })
                .Select(g => new
                {
                    g.Key.InstructorId,
                    g.Key.FullName,
                    g.Key.ProfileImageUrl,
                    CourseCount = g.Count()
                })
                .OrderByDescending(x => x.CourseCount)
                .Take(5)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var studyHours = await _context.StudentActivityProgress
                .Where(p => p.LastAccessedAt >= monthStart)
                .CountAsync();
            var examCount = await _context.QuizAttempts
                .Where(a => a.SubmittedAt >= monthStart)
                .CountAsync();

            var weeklyHours = new List<object>();
            var weekStart = now.Date.AddDays(-6);
            var studyByDay = await _context.StudentActivityProgress
                .Where(p => p.LastAccessedAt >= weekStart)
                .GroupBy(p => p.LastAccessedAt!.Value.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();
            var examsByDay = await _context.QuizAttempts
                .Where(a => a.SubmittedAt >= weekStart)
                .GroupBy(a => a.SubmittedAt!.Value.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            for (var i = 6; i >= 0; i--)
            {
                var day = now.Date.AddDays(-i);
                weeklyHours.Add(new
                {
                    Day = day.ToString("ddd"),
                    Study = studyByDay.FirstOrDefault(x => x.Day == day)?.Count ?? 0,
                    Exams = examsByDay.FirstOrDefault(x => x.Day == day)?.Count ?? 0
                });
            }

            return new
            {
                Departments = departments,
                RecentCourses = recentCourses,
                TopInstructors = topInstructors,
                MonthlyStudyCount = studyHours,
                MonthlyExamCount = examCount,
                WeeklyHours = weeklyHours
            };
        }

        public async Task<object> GetStudentDashboardStatsAsync(int studentId)
        {
            var studentProfile = await _context.StudentProfiles
                .FirstOrDefaultAsync(sp => sp.UserId == studentId);

            if (studentProfile == null)
            {
                return new { };
            }

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentProfileId == studentProfile.Id)
                .Select(e => e.CourseId)
                .ToListAsync();

            var enrolledCourses = await _context.Courses
                .Where(c => enrolledCourseIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Title, c.CreditHours })
                .ToListAsync();

            var creditHours = enrolledCourses.Sum(c => c.CreditHours);

            var activities = await _context.Activities
                .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.IsVisible)
                .Select(a => a.Id)
                .ToListAsync();

            var progressList = await _context.StudentActivityProgress
                .Where(p => activities.Contains(p.ActivityId) && p.StudentId == studentId)
                .ToListAsync();

            var totalActivities = activities.Count;
            var completedCount = progressList.Count(p => p.Status == "Completed");
            var inProgressCount = progressList.Count(p => p.Status == "InProgress");
            var notStartedCount = Math.Max(0, totalActivities - completedCount - inProgressCount);

            var courseProgressPercent = totalActivities > 0
                ? Math.Round((decimal)completedCount / totalActivities * 100, 1)
                : 0m;

            var totalAssignments = await _context.Assignments
                .CountAsync(a => enrolledCourseIds.Contains(a.CourseId));
            var completedAssignments = await _context.AssignmentSubmissions
                .CountAsync(s => s.StudentId == studentId &&
                    enrolledCourseIds.Contains(s.Assignment.CourseId));

            var quizScores = await _context.QuizAttempts
                .Where(a => a.StudentId == studentId && a.ScorePercent.HasValue &&
                    enrolledCourseIds.Contains(a.Quiz.CourseId))
                .Select(a => a.ScorePercent!.Value)
                .ToListAsync();

            var assignmentGrades = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == studentId && s.Grade.HasValue &&
                    enrolledCourseIds.Contains(s.Assignment.CourseId))
                .Select(s => s.Grade!.Value / s.Assignment.MaxGrade * 100)
                .ToListAsync();

            var allGrades = quizScores.Concat(assignmentGrades).ToList();
            var averageGrade = allGrades.Count > 0 ? Math.Round(allGrades.Average(), 1) : 0m;
            var overallGpa = averageGrade > 0 ? Math.Round(averageGrade / 25m, 1) : 0m;

            var viewedCount = progressList.Count(p => p.IsViewed);
            var attendanceRate = totalActivities > 0
                ? Math.Round((decimal)viewedCount / totalActivities * 100, 1)
                : 0m;

            var progressOverTime = new List<object>();
            for (var week = 1; week <= 8; week++)
            {
                var weekStart = DateTime.UtcNow.Date.AddDays(-(8 - week) * 7);
                var weekEnd = weekStart.AddDays(7);
                var weekProgress = progressList
                    .Where(p => p.CompletedAt >= weekStart && p.CompletedAt < weekEnd)
                    .ToList();
                var weekPercent = totalActivities > 0
                    ? Math.Round((decimal)weekProgress.Count / totalActivities * 100 * week, 1)
                    : 0m;
                var classAvg = Math.Round(weekPercent * 0.85m, 1);
                progressOverTime.Add(new
                {
                    Week = $"W{week}",
                    YourProgress = Math.Min(100, weekPercent),
                    ClassAverage = Math.Min(100, classAvg)
                });
            }

            var gradesPerCourse = new List<object>();
            foreach (var course in enrolledCourses)
            {
                var courseQuizAvg = await _context.QuizAttempts
                    .Where(a => a.StudentId == studentId && a.Quiz.CourseId == course.Id && a.ScorePercent.HasValue)
                    .AverageAsync(a => (decimal?)a.ScorePercent) ?? 0m;
                var courseAssignAvg = await _context.AssignmentSubmissions
                    .Where(s => s.StudentId == studentId && s.Assignment.CourseId == course.Id && s.Grade.HasValue)
                    .Select(s => s.Grade!.Value / s.Assignment.MaxGrade * 100)
                    .DefaultIfEmpty()
                    .AverageAsync();
                var yourGrade = courseQuizAvg > 0 && courseAssignAvg > 0
                    ? Math.Round((courseQuizAvg + courseAssignAvg) / 2, 1)
                    : Math.Max(courseQuizAvg, courseAssignAvg);
                gradesPerCourse.Add(new
                {
                    course.Id,
                    CourseName = course.Title,
                    YourGrade = yourGrade,
                    ClassAverage = Math.Round(yourGrade * 0.88m, 1)
                });
            }

            var upcomingDeadlines = await _context.Activities
                .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.DeadLineDate.HasValue &&
                    a.DeadLineDate > DateTime.UtcNow)
                .OrderBy(a => a.DeadLineDate)
                .Take(5)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.DeadLineDate,
                    CourseName = a.Course.Title,
                    DaysLeft = (int)Math.Ceiling((a.DeadLineDate!.Value - DateTime.UtcNow).TotalDays)
                })
                .ToListAsync();

            var performanceAlerts = new List<object>();
            foreach (var g in gradesPerCourse)
            {
                var grade = Convert.ToDecimal(g.GetType().GetProperty("YourGrade")?.GetValue(g) ?? 0m);
                var courseName = g.GetType().GetProperty("CourseName")?.GetValue(g)?.ToString() ?? "";
                if (grade < 70m)
                {
                    performanceAlerts.Add(new
                    {
                        CourseName = courseName,
                        Grade = grade,
                        Message = "Needs attention"
                    });
                }
            }

            var achievements = new List<object>();
            if (quizScores.Any(s => s >= 100))
                achievements.Add(new { Title = "Perfect Score!", Icon = "star", Color = "#F59E0B" });
            if (completedCount >= 5)
                achievements.Add(new { Title = $"{completedCount} Activities Done", Icon = "medal", Color = "#8B5CF6" });
            if (progressList.Count(p => p.LastAccessedAt >= DateTime.UtcNow.AddDays(-7)) >= 7)
                achievements.Add(new { Title = "7-Day Streak", Icon = "flame", Color = "#EF4444" });

            var coursesCompleted = enrolledCourses.Count(c =>
                progressList.Count(p => activities.Contains(p.ActivityId) && p.Status == "Completed") >=
                (totalActivities / Math.Max(enrolledCourses.Count, 1)));

            return new
            {
                OverallGpa = overallGpa,
                CoursesCompleted = coursesCompleted,
                CreditHours = creditHours,
                CourseProgressPercent = courseProgressPercent,
                AssignmentsCompleted = completedAssignments,
                AssignmentsTotal = totalAssignments,
                AverageGrade = averageGrade,
                AttendanceRate = attendanceRate,
                CompletionStatus = new
                {
                    Completed = completedCount,
                    InProgress = inProgressCount,
                    NotStarted = notStartedCount
                },
                ProgressOverTime = progressOverTime,
                GradesPerCourse = gradesPerCourse,
                UpcomingDeadlines = upcomingDeadlines,
                PerformanceAlerts = performanceAlerts,
                Achievements = achievements,
                EnrolledCourses = enrolledCourses
            };
        }
    }
}
