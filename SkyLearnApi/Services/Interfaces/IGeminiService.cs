namespace SkyLearnApi.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<string> SummarizeTextAsync(string content);
        Task<string> SummarizeFileAsync(string filePath, string contentType);
        Task<string> TranscribeFileAsync(string filePath, string contentType);
        Task<string> GenerateQuizQuestionsAsync(string prompt);
        Task<string> GenerateQuizQuestionsWithFileAsync(string prompt, string filePath, string contentType);
        Task<string> TranslateToArabicAsync(string content);
        Task<string> GenerateChatResponseAsync(string systemPrompt, List<SkyLearnApi.DTOs.Chat.ChatMessageDto> history, string newMessage, CancellationToken cancellationToken = default);
    }
}
