namespace SkyLearnApi.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<string> SummarizeTextAsync(string content);
        Task<string> SummarizeFileAsync(string filePath, string contentType);
        Task<string> TranscribeFileAsync(string filePath, string contentType);
        Task<string> GenerateQuizQuestionsAsync(string prompt);
        Task<string> TranslateToArabicAsync(string content);
    }
}
