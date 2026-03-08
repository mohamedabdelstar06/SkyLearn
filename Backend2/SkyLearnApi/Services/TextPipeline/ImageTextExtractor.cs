using Tesseract;
using System.IO;

namespace SkyLearnApi.Services.TextPipeline
{
    public class ImageTextExtractor : ITextExtractor
    {
        private readonly string _tessDataPath;
        private readonly ILogger<ImageTextExtractor> _logger;

        public ImageTextExtractor(IWebHostEnvironment env, ILogger<ImageTextExtractor> logger)
        {
            _logger = logger;
            _tessDataPath = Path.Combine(env.ContentRootPath, "tessdata");
            
            if (!Directory.Exists(_tessDataPath))
            {
                Directory.CreateDirectory(_tessDataPath);
            }
        }

        public bool CanHandle(string contentType) => contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase);

        public async Task<string> ExtractTextAsync(string filePath, string contentType)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(Path.Combine(_tessDataPath, "eng.traineddata")) && !File.Exists(Path.Combine(_tessDataPath, "ara.traineddata")))
                {
                    _logger.LogWarning("Tesseract models not found in {TessDataPath}. Skipping OCR.", _tessDataPath);
                    return "Image text extraction requires Tesseract language packs (eng.traineddata or ara.traineddata) to be downloaded inside the 'tessdata' folder.";
                }

                try
                {
                    var language = File.Exists(Path.Combine(_tessDataPath, "ara.traineddata")) ? "ara" : "eng";
                    using var engine = new TesseractEngine(_tessDataPath, language, EngineMode.Default);
                    using var img = Pix.LoadFromFile(filePath);
                    using var page = engine.Process(img);
                    return page.GetText();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR Processing failed for {FilePath}", filePath);
                    return "Image text extraction failed.";
                }
            });
        }
    }
}
