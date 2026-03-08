using Hangfire;
using Microsoft.AspNetCore.SignalR;
using SkyLearnApi.Data;
using SkyLearnApi.Entities;
using SkyLearnApi.Hubs;
using SkyLearnApi.Services.Interfaces;
using SkyLearnApi.DTOs.Lectures;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SkyLearnApi.Services.Implementation
{
    public class LectureSummarizerJob
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEnumerable<SkyLearnApi.Services.TextPipeline.ITextExtractor> _textExtractors;
        private readonly SkyLearnApi.Services.TextPipeline.ITextCleaner _textCleaner;
        private readonly SkyLearnApi.Services.TextPipeline.ILocalSummarizer _localSummarizer;
        private readonly IActivityService _activityService;
        private readonly ILogger<LectureSummarizerJob> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;

        public LectureSummarizerJob(
            AppDbContext context,
            IWebHostEnvironment env,
            IEnumerable<SkyLearnApi.Services.TextPipeline.ITextExtractor> textExtractors,
            SkyLearnApi.Services.TextPipeline.ITextCleaner textCleaner,
            SkyLearnApi.Services.TextPipeline.ILocalSummarizer localSummarizer,
            IActivityService activityService,
            ILogger<LectureSummarizerJob> logger,
            IHubContext<NotificationHub> hubContext,
            IEmailService emailService)
        {
            _context = context;
            _env = env;
            _textExtractors = textExtractors;
            _textCleaner = textCleaner;
            _localSummarizer = localSummarizer;
            _activityService = activityService;
            _logger = logger;
            _hubContext = hubContext;
            _emailService = emailService;
        }

        public async Task ProcessSummarizeAsync(int lectureId, int userId)
        {
            var lecture = await _context.Lectures.Include(l => l.CreatedBy).FirstOrDefaultAsync(l => l.Id == lectureId);
            if (lecture == null)
            {
                _logger.LogWarning("Lecture {LectureId} not found, summarizer job aborted.", lectureId);
                return;
            }

            try
            {
                _logger.LogInformation("Background Job started: Generating local summary for Lecture {LectureId}. ContentType: {ContentType}", lectureId, lecture.ContentType);

                var filePath = Path.Combine(_env.WebRootPath ?? string.Empty, lecture.FileUrl?.TrimStart('/') ?? string.Empty);
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"The lecture file could not be found: {filePath}");

                var extractor = _textExtractors.FirstOrDefault(e => e.CanHandle(lecture.ContentType))
                    ?? throw new NotSupportedException($"No local text extractor found for content type: {lecture.ContentType}");

                _logger.LogInformation("Extracting text using {ExtractorType}", extractor.GetType().Name);
                var rawText = await extractor.ExtractTextAsync(filePath, lecture.ContentType);

                var isAudioVideo = lecture.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase) ||
                                   lecture.ContentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase);

                if (isAudioVideo)
                {
                    lecture.Transcript = rawText;
                    lecture.TranscriptGeneratedAt = DateTime.UtcNow;
                }
                else
                {
                    lecture.Transcript = "Transcript is not applicable for documents or images.";
                    lecture.TranscriptGeneratedAt = DateTime.UtcNow;
                }

                var cleanedText = _textCleaner.CleanText(rawText);

                _logger.LogInformation("Summarizing extracted text locally...");
                lecture.AiSummary = _localSummarizer.GenerateSummary(cleanedText);
                lecture.SummaryGeneratedAt = DateTime.UtcNow;

                lecture.SummaryStatus = "Completed";
                lecture.EstimatedCompletionMinutes = null;

                await _context.SaveChangesAsync();
                
                // Track activity
                await _activityService.TrackEntityActionAsync(ActivityActions.LectureSummarized, "Lecture", lecture.Id, userId,
                    $"AI summary & transcript generated for lecture '{lecture.Title}'");

                // Notifications
                if (lecture.CreatedBy != null)
                {
                    // 1. SignalR Real-Time Notification
                    var notificationMessage = $"🎉 Your summary for lecture '{lecture.Title}' is ready!";
                    await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notificationMessage);
                    
                    // 2. Email Notification
                    try
                    {
                        await _emailService.SendEmailAsync(lecture.CreatedBy.Email!, "Lecture Summary Ready",
                            $"Hello {lecture.CreatedBy.FullName},<br><br>The local summary for your lecture '<strong>{lecture.Title}</strong>' has been successfully generated and is now available on SkyLearn.<br><br>Happy teaching!");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send summary completion email to {Email}", lecture.CreatedBy.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to summarize lecture locally.");
                lecture.SummaryStatus = "Failed";
                lecture.AiSummary = $"Generation Failed: {ex.Message}";
                lecture.Transcript = $"Generation Failed: {ex.Message}";
                await _context.SaveChangesAsync();

                if (lecture.CreatedBy != null)
                {
                    var errorMessage = $"⚠️ Failed to generate summary for '{lecture.Title}'.";
                    await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", errorMessage);
                }
            }
        }
    }
}
