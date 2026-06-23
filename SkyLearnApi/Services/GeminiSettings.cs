namespace SkyLearnApi.Services
{
    public class GeminiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-flash-latest";
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 120;
    }
}
