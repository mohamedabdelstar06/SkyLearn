using System.Text.RegularExpressions;

namespace SkyLearnApi.Services.TextPipeline
{
    public class TextCleaner : ITextCleaner
    {
        public string CleanText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // 1. Remove Whisper timestamps e.g. [00:00:00.000] -> [00:00:04.000]
            var cleaned = Regex.Replace(input, @"\[\d{2}:\d{2}:\d{2}\.\d{3}\]\s*->\s*\[\d{2}:\d{2}:\d{2}\.\d{3}\]:?", "");
            
            // 2. Remove other common timestamps
            cleaned = Regex.Replace(cleaned, @"\[\d{2}:\d{2}:\d{2}\]", "");

            // 3. Remove weird symbols and unprintable chars
            cleaned = Regex.Replace(cleaned, @"[^\u0000-\u024F\u0600-\u06FF\s\p{P}]", ""); // Keep basic Latin, Extended Latin, Arabic, whitespace, and punctuation

            // 4. Remove duplicate spaces and newlines
            cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");
            cleaned = Regex.Replace(cleaned, @"[ \t]+", " ");

            return cleaned.Trim();
        }
    }
}
