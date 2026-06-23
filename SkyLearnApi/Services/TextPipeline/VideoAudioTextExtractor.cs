using SkyLearnApi.Services.Interfaces;

namespace SkyLearnApi.Services.TextPipeline
{
    public class VideoAudioTextExtractor : ITextExtractor
    {
        private readonly ILocalTranscriptionService _localTranscriptionService;
        private readonly ILogger<VideoAudioTextExtractor> _logger;

        public VideoAudioTextExtractor(ILocalTranscriptionService localTranscriptionService, ILogger<VideoAudioTextExtractor> logger)
        {
            _localTranscriptionService = localTranscriptionService;
            _logger = logger;
        }

        public bool CanHandle(string contentType)
        {
            var ct = contentType.ToLowerInvariant();
            return ct.StartsWith("video") || ct.StartsWith("audio");
        }

        public async Task<string> ExtractTextAsync(string filePath, string contentType)
        {
            _logger.LogInformation("Extracting audio from {FilePath} to WAV format...", filePath);
            var tempWavPath = await _localTranscriptionService.ConvertToWavAsync(filePath);
            
            try
            {
                _logger.LogInformation("Generating local AI transcript using Whisper model...");
                var transcript = await _localTranscriptionService.TranscribeAudioAsync(tempWavPath);
                return transcript;
            }
            finally
            {
                if (System.IO.File.Exists(tempWavPath))
                {
                    System.IO.File.Delete(tempWavPath);
                }
            }
        }
    }
}
