using SkyLearnApi.DTOs.Assignments;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IAssignmentService
    {
        Task<AssignmentResponseDto> CreateAsync(int courseId, CreateAssignmentDto dto, int userId);
        Task<List<AssignmentResponseDto>> GetByCourseAsync(int courseId, int userId, string userRole);
        Task<AssignmentResponseDto?> GetByIdAsync(int id, int userId, string userRole);
        Task<AssignmentResponseDto?> UpdateAsync(int id, UpdateAssignmentDto dto, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<AssignmentSubmissionResponseDto> SubmitAsync(int assignmentId, IFormFile file, int studentId);
        Task<AssignmentSubmissionResponseDto> GradeAsync(int assignmentId, int studentId, GradeAssignmentDto dto, int graderId);
        Task<List<AssignmentSubmissionResponseDto>> GetSubmissionsAsync(int assignmentId, int userId, string userRole);
    }
}
