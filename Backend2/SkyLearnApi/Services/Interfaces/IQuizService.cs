using SkyLearnApi.DTOs.Quizzes;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IQuizService
    {
        Task<QuizResponseDto> CreateAsync(int courseId, CreateQuizDto dto, int userId);
        Task<List<QuizResponseDto>> GetByCourseAsync(int courseId, int userId, string userRole);
        Task<QuizResponseDto?> GetByIdAsync(int id, int userId, string userRole);
        Task<QuizResponseDto?> UpdateAsync(int id, UpdateQuizDto dto, int userId);
        Task<bool> DeleteAsync(int id, int userId);
        Task<QuizResponseDto> GenerateWithAiAsync(GenerateQuizDto dto, int userId);
        Task<QuizTakeResponseDto> TakeQuizAsync(int quizId, int studentId);
        Task<QuizResultResponseDto> SubmitQuizAsync(int quizId, SubmitQuizDto dto, int studentId);
        Task<QuizStudentResultsDto> GetQuizResultsAsync(int quizId, int userId, string userRole);
        Task<QuizResultResponseDto?> GetMyResultAsync(int quizId, int studentId);
        Task<QuizResultResponseDto> GradeQuizAsync(int quizId, GradeQuizDto dto, int graderId);
        Task TranslateQuizAsync(int quizId);
    }
}
