using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace SkyLearnApi.Services.Implementation
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IOptions<GeminiSettings> settings, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public async Task<string> SummarizeTextAsync(string content)
        {
            var prompt = $@"You are an educational content summarizer. Summarize the following content:
1. Provide a concise summary (200-300 words)
2. List key concepts and definitions
3. Highlight important facts or formulas
Respond in the same language as the content.

Content:
{content}";

            return await CallGeminiAsync(prompt);
        }

        public async Task<string> SummarizeFileAsync(string filePath, string contentType)
        {
            var mimeType = GetMimeType(filePath, contentType);
            var fileUri = await UploadFileToGeminiAsync(filePath, mimeType);

            var prompt = @"You are an educational content summarizer. Analyze this file and provide:
1. A concise summary (200-300 words)
2. Key concepts and definitions
3. Important facts, formulas, or points
Respond in the same language as the content.";

            return await CallGeminiWithFileUriAsync(prompt, fileUri, mimeType);
        }

        public async Task<string> TranscribeFileAsync(string filePath, string contentType)
        {
            var mimeType = GetMimeType(filePath, contentType);
            var fileUri = await UploadFileToGeminiAsync(filePath, mimeType);

            var prompt = @"Transcribe this media content. Provide:
1. Full transcript with timestamps where possible
2. Speaker identification if multiple speakers
Respond in the same language as the spoken content.";

            return await CallGeminiWithFileUriAsync(prompt, fileUri, mimeType);
        }

        public async Task<string> GenerateQuizQuestionsAsync(string prompt)
        {
            return await CallGeminiAsync(prompt);
        }

        public async Task<string> TranslateToArabicAsync(string content)
        {
            var prompt = $@"Translate the following educational content from English to Arabic. 
Keep any technical terms that are commonly used in English as-is.
Return only the translation, nothing else.

Content to translate:
{content}";

            return await CallGeminiAsync(prompt);
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 8192
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            for (int attempt = 0; attempt < _settings.MaxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Gemini API error (attempt {Attempt}): {Status} {Body}",
                            attempt + 1, response.StatusCode, responseBody);

                        if (attempt == _settings.MaxRetries - 1)
                            throw new Exception($"Gemini API failed after {_settings.MaxRetries} attempts: {responseBody}");

                        // Exponential backoff for 429 Too Many Requests
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            var backoffSeconds = Math.Pow(2, attempt + 1) * 15; // e.g. 30s, 60s, 120s
                            _logger.LogWarning("Rate limit hit (429). Applying exponential backoff, waiting {DelaySeconds} seconds...", backoffSeconds);
                            await Task.Delay(TimeSpan.FromSeconds(backoffSeconds));
                        }
                        else
                        {
                            await Task.Delay(1000 * (attempt + 1));
                        }
                        continue;
                    }

                    return ExtractTextFromResponse(responseBody);
                }
                catch (HttpRequestException ex) when (attempt < _settings.MaxRetries - 1)
                {
                    _logger.LogWarning(ex, "Gemini API request failed (attempt {Attempt})", attempt + 1);
                    await Task.Delay(1000 * (attempt + 1));
                }
            }

            throw new Exception("Gemini API failed after all retry attempts");
        }

        private async Task<string> UploadFileToGeminiAsync(string filePath, string mimeType)
        {
            var fileInfo = new FileInfo(filePath);
            var fileSizeBytes = fileInfo.Length;
            
            // 1. Initial Resumable request
            var startUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={_settings.ApiKey}";
            var startRequest = new HttpRequestMessage(HttpMethod.Post, startUrl);
            startRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            startRequest.Headers.Add("X-Goog-Upload-Command", "start");
            startRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", fileSizeBytes.ToString());
            startRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);

            var metadata = new { file = new { display_name = Path.GetFileName(filePath) } };
            startRequest.Content = new StringContent(JsonSerializer.Serialize(metadata), System.Text.Encoding.UTF8, "application/json");
            
            var startResponse = await _httpClient.SendAsync(startRequest);
            var startBody = await startResponse.Content.ReadAsStringAsync();
            if (!startResponse.IsSuccessStatusCode)
                throw new Exception($"Gemini File API start failed: {startBody}");

            if (!startResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var values))
                throw new Exception("Failed to get Google Upload URL.");

            var uploadUrl = values.FirstOrDefault();
            if (string.IsNullOrEmpty(uploadUrl)) throw new Exception("Upload URL is empty.");

            // 2. Upload actual file chunk (streaming so memory doesn't spike!)
            using var fileStream = File.OpenRead(filePath);
            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
            uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            uploadRequest.Content = streamContent;

            // Use a separate HTTP Client that doesn't share global tight timeouts if uploading big videos
            using var uploaderClient = new HttpClient();
            uploaderClient.Timeout = TimeSpan.FromMinutes(15);
            var uploadResponse = await uploaderClient.SendAsync(uploadRequest);
            var uploadBody = await uploadResponse.Content.ReadAsStringAsync();
            
            if (!uploadResponse.IsSuccessStatusCode)
                throw new Exception($"Gemini File API upload failed: {uploadBody}");

            using var doc = JsonDocument.Parse(uploadBody);
            var fileNode = doc.RootElement.GetProperty("file");
            var uri = fileNode.GetProperty("uri").GetString();
            var name = fileNode.GetProperty("name").GetString();
            
            _logger.LogInformation("Successfully uploaded file to Gemini. Name: {Name}", name);

            // 3. Poll for processing completion if video
            if (mimeType.StartsWith("video"))
            {
                var checkUrl = $"https://generativelanguage.googleapis.com/v1beta/{name}?key={_settings.ApiKey}";
                for (int i = 0; i < 60; i++)
                {
                    await Task.Delay(2000); // Poll every 2 seconds
                    var checkResp = await _httpClient.GetAsync(checkUrl);
                    var checkJson = await checkResp.Content.ReadAsStringAsync();
                    using var checkDoc = JsonDocument.Parse(checkJson);
                    var state = checkDoc.RootElement.GetProperty("state").GetString();
                    
                    if (state == "ACTIVE") break;
                    if (state == "FAILED") throw new Exception("Gemini video processing failed internally.");
                }
            }

            return uri ?? "";
        }

        private async Task<string> CallGeminiWithFileUriAsync(string prompt, string fileUri, string mimeType)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                file_data = new
                                {
                                    mime_type = mimeType,
                                    file_uri = fileUri
                                }
                            },
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 8192
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            for (int attempt = 0; attempt < _settings.MaxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Gemini API file error (attempt {Attempt}): {Status} {Body}",
                            attempt + 1, response.StatusCode, responseBody);

                        if (attempt == _settings.MaxRetries - 1)
                            throw new Exception($"Gemini API failed: {responseBody}");

                        // Exponential backoff for 429 Too Many Requests
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            var backoffSeconds = Math.Pow(2, attempt + 1) * 15; // e.g. 30s, 60s, 120s
                            _logger.LogWarning("Rate limit hit (429). Applying exponential backoff, waiting {DelaySeconds} seconds...", backoffSeconds);
                            await Task.Delay(TimeSpan.FromSeconds(backoffSeconds));
                        }
                        else
                        {
                            await Task.Delay(1000 * (attempt + 1));
                        }
                        continue;
                    }

                    return ExtractTextFromResponse(responseBody);
                }
                catch (HttpRequestException ex) when (attempt < _settings.MaxRetries - 1)
                {
                    _logger.LogWarning(ex, "Gemini file request failed (attempt {Attempt})", attempt + 1);
                    await Task.Delay(1000 * (attempt + 1));
                }
            }

            throw new Exception("Gemini API failed after all retry attempts");
        }

        private static string ExtractTextFromResponse(string responseJson)
        {
            using var doc = JsonDocument.Parse(responseJson);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
                return string.Empty;

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            var textParts = new List<string>();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                    textParts.Add(text.GetString() ?? string.Empty);
            }
            return string.Join("\n", textParts);
        }

        private static string GetMimeType(string filePath, string contentType)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".webm" => "video/webm",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
    }
}
