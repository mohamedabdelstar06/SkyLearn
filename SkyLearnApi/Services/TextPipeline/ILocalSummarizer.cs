namespace SkyLearnApi.Services.TextPipeline
{
    public interface ILocalSummarizer
    {
        string GenerateSummary(string text);
    }
}
