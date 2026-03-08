using SkyLearnApi.DTOs.Lectures;
using Hangfire;

namespace SkyLearnApi.Services.Implementation
{
    public class LectureService : ILectureService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEnumerable<SkyLearnApi.Services.TextPipeline.ITextExtractor> _textExtractors;
        private readonly SkyLearnApi.Services.TextPipeline.ITextCleaner _textCleaner;
        private readonly SkyLearnApi.Services.TextPipeline.ILocalSummarizer _localSummarizer;
        private readonly IActivityService _activityService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LectureService> _logger;
        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;

        public LectureService(
            AppDbContext context, 
            IWebHostEnvironment env,
            IEnumerable<SkyLearnApi.Services.TextPipeline.ITextExtractor> textExtractors,
            SkyLearnApi.Services.TextPipeline.ITextCleaner textCleaner,
            SkyLearnApi.Services.TextPipeline.ILocalSummarizer localSummarizer,
            IActivityService activityService, 
            INotificationService notificationService, 
            ILogger<LectureService> logger,
            Hangfire.IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _env = env;
            _textExtractors = textExtractors;
            _textCleaner = textCleaner;
            _localSummarizer = localSummarizer;
            _activityService = activityService;
            _notificationService = notificationService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<LectureResponseDto> CreateAsync(int courseId, CreateLectureDto dto, int userId)
        {
            _logger.LogInformation("Creating lecture in course {CourseId}. Title: {Title}, UserId: {UserId}, HasFile: {HasFile}",
                courseId, dto.Title, userId, dto.File != null);

            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException($"Course with ID {courseId} not found.");

            var lecture = new Lecture
            {
                CourseId = courseId,
                Title = dto.Title,
                Description = dto.Description,
                CreatedById = userId
            };

            if (dto.File != null)
            {
                lecture.FileUrl = await FileHelper.SaveFileAsync(dto.File, $"lectures/{courseId}", _env);
                lecture.ContentType = FileHelper.DetectContentType(dto.File.FileName);
                _logger.LogDebug("Lecture file saved: {FileUrl}, ContentType: {ContentType}", lecture.FileUrl, lecture.ContentType);
            }
            else
            {
                lecture.FileUrl = null;
                lecture.ContentType = null;
            }

            if (dto.AdditionalFiles != null && dto.AdditionalFiles.Any())
            {
                var urls = new List<string>();
                foreach (var file in dto.AdditionalFiles)
                {
                    urls.Add(await FileHelper.SaveFileAsync(file, $"lectures/{courseId}", _env));
                }
                lecture.AdditionalFileUrls = JsonSerializer.Serialize(urls);
                if (lecture.ContentType == "Video" || lecture.ContentType == "Pdf")
                    lecture.ContentType = "Mixed";
                _logger.LogDebug("Saved {Count} additional files for lecture", urls.Count);
            }

            _context.Lectures.Add(lecture);

try
{
    await _context.SaveChangesAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "DB ERROR while creating lecture");
    throw; 
}
            _logger.LogInformation("Lecture created successfully. LectureId: {LectureId}, CourseId: {CourseId}, Title: {Title}",
                lecture.Id, courseId, lecture.Title);

            await _activityService.TrackEntityActionAsync(ActivityActions.LectureCreated, "Lecture", lecture.Id, userId,
                $"Lecture '{lecture.Title}' created in course '{course.Title}'");

            // Notify enrolled students about new lecture
            await _notificationService.NotifyEnrolledStudentsAsync(courseId,
                "New Lecture Available",
                $"A new lecture '{lecture.Title}' has been added to '{course.Title}'.",
                "NewLecture", lecture.Id);

            return await MapToResponseDto(lecture);
        }

        public async Task<List<LectureResponseDto>> GetByCourseAsync(int courseId, int userId, string userRole)
        {
            var query = _context.Lectures
                .Where(l => l.CourseId == courseId)
                .Include(l => l.CreatedBy)
                .OrderBy(l => l.CreatedAt)
                .AsQueryable();

            // Students only see visible lectures
            if (userRole == Roles.Student)
                query = query.Where(l => l.IsVisible);

            var lectures = await query.ToListAsync();

            // Batch load student progress for all lectures if user is a student
            Dictionary<int, StudentActivityProgress>? progressMap = null;
            if (userRole == Roles.Student)
            {
                var lectureIds = lectures.Select(l => l.Id).ToList();
                progressMap = await _context.StudentActivityProgress
                    .Where(p => p.StudentId == userId && lectureIds.Contains(p.ActivityId))
                    .ToDictionaryAsync(p => p.ActivityId);
            }

            var result = new List<LectureResponseDto>();
            foreach (var l in lectures)
            {
                var dto = await MapToResponseDto(l);
                if (progressMap != null && progressMap.TryGetValue(l.Id, out var progress))
                {
                    dto.IsViewed = progress.Status == "Completed" || progress.Status == "InProgress";
                    dto.ViewedAt = progress.FirstAccessedAt;
                }
                else if (userRole == Roles.Student)
                {
                    dto.IsViewed = false;
                    dto.ViewedAt = null;
                }
                result.Add(dto);
            }
            return result;
        }

        public async Task<LectureResponseDto?> GetByIdAsync(int id, int userId, string userRole)
        {
            var lecture = await _context.Lectures
                .Include(l => l.CreatedBy)
                .Include(l => l.Comments)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lecture == null) return null;
            if (userRole == Roles.Student && !lecture.IsVisible) return null;

            // Track view in StudentActivityProgress for students
            if (userRole == Roles.Student)
            {
                var progress = await _context.StudentActivityProgress
                    .FirstOrDefaultAsync(p => p.ActivityId == id && p.StudentId == userId);

                if (progress == null)
                {
                    progress = new StudentActivityProgress
                    {
                        ActivityId = id,
                        StudentId = userId,
                        Status = "Completed",
                        ProgressPercent = 100,
                        FirstAccessedAt = DateTime.UtcNow,
                        LastAccessedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                    _context.StudentActivityProgress.Add(progress);
                }
                else
                {
                    progress.LastAccessedAt = DateTime.UtcNow;
                    if (progress.Status == "NotStarted")
                    {
                        progress.Status = "Completed";
                        progress.ProgressPercent = 100;
                        progress.CompletedAt = DateTime.UtcNow;
                    }
                }
                await _context.SaveChangesAsync();
            }

            await _activityService.TrackEntityActionAsync(ActivityActions.LectureViewed, "Lecture", lecture.Id, userId,
                $"Lecture '{lecture.Title}' viewed");

            var dto = await MapToResponseDto(lecture);

            // Populate view status for students
            if (userRole == Roles.Student)
            {
                var studentProgress = await _context.StudentActivityProgress
                    .FirstOrDefaultAsync(p => p.ActivityId == id && p.StudentId == userId);
                if (studentProgress != null)
                {
                    dto.IsViewed = true;
                    dto.ViewedAt = studentProgress.FirstAccessedAt;
                }
            }

            return dto;
        }

        public async Task<LectureResponseDto?> UpdateAsync(int id, UpdateLectureDto dto, int userId)
        {
            var lecture = await _context.Lectures.FindAsync(id);
            if (lecture == null) return null;

            if (dto.Title != null) lecture.Title = dto.Title;
            if (dto.Description != null) lecture.Description = dto.Description;

            if (dto.File != null)
            {
                if (!string.IsNullOrEmpty(lecture.FileUrl))
                    FileHelper.DeleteFile(lecture.FileUrl, _env);
                lecture.FileUrl = await FileHelper.SaveFileAsync(dto.File, $"lectures/{lecture.CourseId}", _env);
                lecture.ContentType = FileHelper.DetectContentType(dto.File.FileName);
                // Clear cached AI content when file changes
                lecture.AiSummary = null;
                lecture.Transcript = null;
                lecture.SummaryGeneratedAt = null;
                lecture.TranscriptGeneratedAt = null;
            }

            if (dto.AdditionalFiles != null && dto.AdditionalFiles.Any())
            {
                // Delete old additional files
                if (!string.IsNullOrEmpty(lecture.AdditionalFileUrls))
                {
                    var oldUrls = JsonSerializer.Deserialize<List<string>>(lecture.AdditionalFileUrls);
                    if (oldUrls != null)
                        foreach (var url in oldUrls)
                            FileHelper.DeleteFile(url, _env);
                }

                var urls = new List<string>();
                foreach (var file in dto.AdditionalFiles)
                    urls.Add(await FileHelper.SaveFileAsync(file, $"lectures/{lecture.CourseId}", _env));
                lecture.AdditionalFileUrls = JsonSerializer.Serialize(urls);
            }

            lecture.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.LectureUpdated, "Lecture", lecture.Id, userId,
                $"Lecture '{lecture.Title}' updated");

            return await MapToResponseDto(lecture);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var lecture = await _context.Lectures.FindAsync(id);
            if (lecture == null) return false;

            var lectureTitle = lecture.Title;
            var courseId = lecture.CourseId;

            if (!string.IsNullOrEmpty(lecture.FileUrl))
                FileHelper.DeleteFile(lecture.FileUrl, _env);

            if (!string.IsNullOrEmpty(lecture.AdditionalFileUrls))
            {
                var urls = JsonSerializer.Deserialize<List<string>>(lecture.AdditionalFileUrls);
                if (urls != null)
                    foreach (var url in urls)
                        FileHelper.DeleteFile(url, _env);
            }

            _context.Lectures.Remove(lecture);
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.LectureDeleted, "Lecture", id, userId,
                $"Lecture '{lectureTitle}' deleted from course {courseId}");

            return true;
        }

        public async Task<LectureSummaryResponseDto> SummarizeAsync(int id, int userId)
        {
            var lecture = await _context.Lectures.FindAsync(id)
                ?? throw new ArgumentException("Lecture not found.");

            // Return cached results if both summary and transcript already exist
            if (lecture.SummaryStatus == "Completed" && !string.IsNullOrEmpty(lecture.AiSummary) && !string.IsNullOrEmpty(lecture.Transcript))
            {
                return new LectureSummaryResponseDto
                {
                    LectureId = lecture.Id,
                    Title = lecture.Title,
                    AiSummary = lecture.AiSummary,
                    Transcript = lecture.Transcript,
                    SummaryGeneratedAt = lecture.SummaryGeneratedAt,
                    TranscriptGeneratedAt = lecture.TranscriptGeneratedAt,
                    Status = "Completed",
                    Message = "✅ Summary is ready!"
                };
            }

            if (lecture.SummaryStatus == "Pending")
            {
                var remaining = lecture.EstimatedCompletionMinutes ?? 10;
                return new LectureSummaryResponseDto
                {
                    LectureId = lecture.Id,
                    Title = lecture.Title,
                    Status = "Pending",
                    Message = $"⏳ Summary is currently being generated. Estimated wait time: ~{remaining} minutes."
                };
            }

            if (string.IsNullOrEmpty(lecture.FileUrl))
                throw new ArgumentException("This lecture has no file uploaded. Please upload a file first.");

            var filePath = Path.Combine(_env.WebRootPath ?? string.Empty, lecture.FileUrl.TrimStart('/'));
            if (!File.Exists(filePath))
                throw new ArgumentException($"The lecture file could not be found on the server.");

            var isAudioVideo = lecture.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase) || 
                               lecture.ContentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase);

            var estimatedMinutes = isAudioVideo ? 25 : 5; // Long estimated default buffer times
            
            lecture.SummaryStatus = "Pending";
            lecture.EstimatedCompletionMinutes = estimatedMinutes;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Enqueuing background job for Lecture {LectureId}.", id);
            _backgroundJobClient.Enqueue<LectureSummarizerJob>(j => j.ProcessSummarizeAsync(id, userId));

            return new LectureSummaryResponseDto
            {
                LectureId = lecture.Id,
                Title = lecture.Title,
                Status = "Pending",
                Message = $"⏳ Summary request is queued and running in the background. Generating may take {estimatedMinutes} minutes depending on lecture size. We'll send an email & notification when ready!",
                EstimatedCompletionMinutes = estimatedMinutes
            };
        }


        private async Task<LectureResponseDto> MapToResponseDto(Lecture lecture)
        {
            if (lecture.CreatedBy == null)
                await _context.Entry(lecture).Reference(l => l.CreatedBy).LoadAsync();

            return new LectureResponseDto
            {
                Id = lecture.Id,
                CourseId = lecture.CourseId,
                Title = lecture.Title,
                Description = lecture.Description,
                ContentType = lecture.ContentType,
                FileUrl = lecture.FileUrl,
                AdditionalFileUrls = lecture.AdditionalFileUrls,
                ThumbnailUrl = lecture.ThumbnailUrl,
                HasSummary = !string.IsNullOrEmpty(lecture.AiSummary),
                HasTranscript = !string.IsNullOrEmpty(lecture.Transcript),
                CreatedById = lecture.CreatedById,
                CreatedByName = lecture.CreatedBy?.FullName ?? string.Empty,
                CreatedAt = lecture.CreatedAt,
                UpdatedAt = lecture.UpdatedAt,
                CommentCount = lecture.Comments?.Count(c => !c.IsDeleted) ?? 0
            };
        }
    }
}
