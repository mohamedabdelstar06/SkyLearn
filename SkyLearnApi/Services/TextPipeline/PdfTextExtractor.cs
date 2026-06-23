using UglyToad.PdfPig;

namespace SkyLearnApi.Services.TextPipeline
{
    public class PdfTextExtractor : ITextExtractor
    {
        public bool CanHandle(string contentType) => contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase);

        public async Task<string> ExtractTextAsync(string filePath, string contentType)
        {
            return await Task.Run(() =>
            {
                using var document = PdfDocument.Open(filePath);
                var text = new System.Text.StringBuilder();
                foreach (var page in document.GetPages())
                {
                    text.AppendLine(page.Text);
                }
                return text.ToString();
            });
        }
    }
}
