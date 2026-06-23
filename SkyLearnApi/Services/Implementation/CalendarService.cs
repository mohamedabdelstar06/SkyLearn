using Microsoft.EntityFrameworkCore;
using SkyLearnApi.Data;
using SkyLearnApi.DTOs.Activities;
using SkyLearnApi.Entities;

namespace SkyLearnApi.Services.Implementation
{
    public class CalendarService : Interfaces.ICalendarService
    {
        private readonly AppDbContext _context;

        public CalendarService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CalendarEventDto>> GetMonthlyEventsAsync(int studentId, int year, int month)
        {
            var enrollmentCourseIds = await _context.Enrollments
                .Where(e => e.StudentProfile.UserId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            return await GetEventsByDateRangeAsync(studentId, enrollmentCourseIds, startDate, endDate);
        }

        public async Task<List<CalendarEventDto>> GetDailyEventsAsync(int studentId, DateTime date)
        {
            var enrollmentCourseIds = await _context.Enrollments
                .Where(e => e.StudentProfile.UserId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await GetEventsByDateRangeAsync(studentId, enrollmentCourseIds, date.Date, date.Date.AddDays(1).AddTicks(-1));
        }

        private async Task<List<CalendarEventDto>> GetEventsByDateRangeAsync(int studentId, List<int> courseIds, DateTime startDate, DateTime endDate)
        {
            var activities = await _context.Activities
                .Include(a => a.Course)
                .Where(a => courseIds.Contains(a.CourseId) && a.IsVisible)
                .Where(a => a.DeadLineDate >= startDate && a.DeadLineDate <= endDate)
                .ToListAsync();

            var completionDict = await _context.StudentActivityProgress
                .Where(p => p.StudentId == studentId && p.Status == "Completed")
                .Select(p => p.ActivityId)
                .ToListAsync();

            var completedSet = new HashSet<int>(completionDict);

            var events = new List<CalendarEventDto>();
            foreach (var activity in activities)
            {
                var isQuiz = activity is Quiz;
                var isAssignment = activity is Assignment;

                if (!isQuiz && !isAssignment) continue; 

                events.Add(new CalendarEventDto
                {
                    ActivityId = activity.Id,
                    Title = activity.Title,
                    ActivityType = isQuiz ? "Quiz" : "Assignment",
                    CourseId = activity.CourseId,
                    CourseName = activity.Course?.Title ?? "",
                    DeadLineDate = activity.DeadLineDate ?? DateTime.MinValue,
                    StartDate = activity.StartDate,
                    IsCompleted = completedSet.Contains(activity.Id),
                    ColorCode = isQuiz ? "#9333EA" : "#3B82F6" 
                });
            }

            return events.OrderBy(e => e.DeadLineDate).ToList();
        }
    }
}
