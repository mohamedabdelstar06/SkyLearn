namespace SkyLearnApi.Services.TextPipeline
{
    public interface ITextExtractor
    {
        bool CanHandle(string contentType);
        Task<string> ExtractTextAsync(string filePath, string contentType);
    }
}
