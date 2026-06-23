using SkyLearnApi.DTOs.Activities;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IStudentActivityService
    {
        Task<PagedStudentActivityResponse> GetMyActivitiesAsync(int studentId, StudentActivityFilterParams filter);
        Task<PagedCourseGradesDto> GetCourseGradesAsync(int courseId, int userId, string userRole, CourseGradesFilterParams filter);
        Task<GradeRecordDto> GetMyGradesAsync(int courseId, int studentId);
    }
}
