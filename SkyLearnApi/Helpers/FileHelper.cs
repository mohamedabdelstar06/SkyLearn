namespace SkyLearnApi.Helpers
{
    public static class FileHelper
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg"
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm"
        };

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a"
        };

        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt"
        };

        public static async Task<string> SaveFileAsync(IFormFile file, string folderName, IWebHostEnvironment environment)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            var folderPath = Path.Combine(environment.WebRootPath, "uploads", folderName);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folderName}/{fileName}";
        }

        public static void DeleteFile(string fileUrl, IWebHostEnvironment environment)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            var fullPath = Path.Combine(environment.WebRootPath, fileUrl.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        public static string DetectContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            if (VideoExtensions.Contains(ext)) return "Video";
            if (DocumentExtensions.Contains(ext)) return "Pdf";
            if (AudioExtensions.Contains(ext)) return "Audio";
            if (ImageExtensions.Contains(ext)) return "Image";
            return "Unknown";
        }

        public static bool IsAllowedExtension(string fileName, string allowedExtensions)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) return false;

            var allowed = allowedExtensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.StartsWith('.') ? e : $".{e}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return allowed.Contains(ext);
        }
    }
}
