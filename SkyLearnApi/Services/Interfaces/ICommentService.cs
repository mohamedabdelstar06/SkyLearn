using SkyLearnApi.DTOs.Comments;

namespace SkyLearnApi.Services.Interfaces
{
    public interface ICommentService
    {
        Task<List<CommentResponseDto>> GetByLectureAsync(int lectureId, int currentUserId);
        Task<CommentResponseDto> CreateAsync(int lectureId, CreateCommentDto dto, int userId);
        Task<CommentResponseDto?> UpdateAsync(int commentId, UpdateCommentDto dto, int userId);
        Task<bool> DeleteAsync(int commentId, int userId);
        Task<bool> ToggleLikeAsync(int commentId, int userId);
    }
}
