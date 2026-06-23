using SkyLearnApi.DTOs.Activities;

namespace SkyLearnApi.Services.Interfaces
{
    public interface ICalendarService
    {
        Task<List<CalendarEventDto>> GetMonthlyEventsAsync(int studentId, int year, int month);
        Task<List<CalendarEventDto>> GetDailyEventsAsync(int studentId, DateTime date);
    }
}
