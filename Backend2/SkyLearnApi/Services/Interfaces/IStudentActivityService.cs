using SkyLearnApi.DTOs.Activities;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IStudentActivityService
    {
        Task<PagedStudentActivityResponse> GetMyActivitiesAsync(int studentId, StudentActivityFilterParams filter);
        Task StartSessionAsync(int activityId, int studentId);
        Task HeartbeatAsync(int activityId, int studentId, HeartbeatDto dto);
        Task EndSessionAsync(int activityId, int studentId);
        Task<GradeRecordDto> GetCourseGradesAsync(int courseId, int userId, string userRole);
        Task<GradeRecordDto> GetMyGradesAsync(int courseId, int studentId);
    }
}
