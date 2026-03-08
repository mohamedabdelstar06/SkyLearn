using SkyLearnApi.DTOs.Lectures;

namespace SkyLearnApi.Services.Interfaces
{
    public interface ILectureService
    {
        Task<LectureResponseDto> CreateAsync(int courseId, CreateLectureDto dto, int userId);
        Task<List<LectureResponseDto>> GetByCourseAsync(int courseId, int userId, string userRole);
        Task<LectureResponseDto?> GetByIdAsync(int id, int userId, string userRole);
        Task<LectureResponseDto?> UpdateAsync(int id, UpdateLectureDto dto, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<LectureSummaryResponseDto> SummarizeAsync(int id, int userId);
    }
}
