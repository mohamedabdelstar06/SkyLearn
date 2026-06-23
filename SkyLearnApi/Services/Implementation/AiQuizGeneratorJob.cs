using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkyLearnApi.Data;
using SkyLearnApi.Entities;
using SkyLearnApi.Hubs;
using SkyLearnApi.Services.Interfaces;
using SkyLearnApi.DTOs.Quizzes;
using System.Text;
using System.Text.Json;

namespace SkyLearnApi.Services.Implementation
{
    public class AiQuizGeneratorJob
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly INotificationService _notificationService;
        private readonly IActivityService _activityService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEnumerable<SkyLearnApi.Services.TextPipeline.ITextExtractor> _textExtractors;
        private readonly ILogger<AiQuizGeneratorJob> _logger;

        public AiQuizGeneratorJob(
            AppDbContext context,
            IGeminiService geminiService,
            INotificationService notificationService,
            IActivityService activityService,
            IEmailService emailService,
            IWebHostEnvironment env,
            IHubContext<NotificationHub> hubContext,
            IEnumerable<SkyLearnApi.Services.TextPipeline.ITextExtractor> textExtractors,
            ILogger<AiQuizGeneratorJob> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _notificationService = notificationService;
            _activityService = activityService;
            _emailService = emailService;
            _env = env;
            _hubContext = hubContext;
            _textExtractors = textExtractors;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)] // Don't retry AI jobs – they're expensive
        public async Task ProcessGenerateAsync(int quizId, int userId)
        {
            // Load the pending quiz shell that was created before job was queued
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
            {
                _logger.LogWarning("AiQuizGeneratorJob: Quiz {QuizId} not found, job aborted.", quizId);
                return;
            }

            // Load the creator
            var creator = await _context.Users.FindAsync(userId);

            try
            {
                _logger.LogInformation("AiQuizGeneratorJob started for QuizId: {QuizId}, UserId: {UserId}", quizId, userId);

                // Deserialize the stored generation parameters from AiPromptUsed field
                var jobParams = JsonSerializer.Deserialize<AiQuizJobParams>(quiz.AiPromptUsed ?? "{}")
                    ?? throw new Exception("Failed to read quiz generation parameters.");

                // Build source content
                var sourceContent = new StringBuilder();

                if (jobParams.LectureIds != null && jobParams.LectureIds.Any())
                {
                    var lectures = await _context.Lectures
                        .Where(l => jobParams.LectureIds.Contains(l.Id))
                        .ToListAsync();

                    foreach (var lecture in lectures)
                    {
                        sourceContent.AppendLine($"--- Lecture: {lecture.Title} ---");

                        // Only use transcript if it contains real content (skip placeholder messages)
                        var hasRealTranscript = !string.IsNullOrWhiteSpace(lecture.Transcript)
                            && !lecture.Transcript.StartsWith("Transcript is not applicable", StringComparison.OrdinalIgnoreCase)
                            && !lecture.Transcript.StartsWith("Generation Failed", StringComparison.OrdinalIgnoreCase);

                        var hasRealSummary = !string.IsNullOrWhiteSpace(lecture.AiSummary)
                            && !lecture.AiSummary.StartsWith("Generation Failed", StringComparison.OrdinalIgnoreCase);

                        if (hasRealTranscript)
                        {
                            sourceContent.AppendLine(lecture.Transcript);
                            _logger.LogInformation("Using transcript for lecture {LectureId}", lecture.Id);
                        }
                        else if (hasRealSummary)
                        {
                            sourceContent.AppendLine(lecture.AiSummary);
                            _logger.LogInformation("Using AI summary for lecture {LectureId}", lecture.Id);
                        }
                        else if (!string.IsNullOrEmpty(lecture.FileUrl))
                        {
                            // Extract text directly from the file
                            var fullPath = Path.Combine(_env.WebRootPath, lecture.FileUrl.TrimStart('/'));
                            if (File.Exists(fullPath))
                            {
                                var contentType = lecture.ContentType ?? Path.GetExtension(lecture.FileUrl);
                                var extractor = _textExtractors.FirstOrDefault(e => e.CanHandle(contentType));
                                if (extractor != null)
                                {
                                    try
                                    {
                                        var text = await extractor.ExtractTextAsync(fullPath, contentType);
                                        if (!string.IsNullOrWhiteSpace(text))
                                        {
                                            sourceContent.AppendLine(text);
                                            _logger.LogInformation("Extracted {Chars} chars from file for lecture {LectureId}", text.Length, lecture.Id);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("File extraction returned empty text for lecture {LectureId} ({FileUrl})", lecture.Id, lecture.FileUrl);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to extract text from lecture file {FileUrl}", lecture.FileUrl);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No text extractor found for content type '{ContentType}' on lecture {LectureId}", contentType, lecture.Id);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Lecture file not found on disk: {FullPath}", fullPath);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Lecture {LectureId} ('{Title}') has no usable content: no transcript, no summary, and no file.", lecture.Id, lecture.Title);
                        }
                    }
                }

                string? validPdfPath = null;
                if (!string.IsNullOrEmpty(jobParams.TempPdfPath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, jobParams.TempPdfPath.TrimStart('/'));
                    if (File.Exists(fullPath))
                    {
                        validPdfPath = fullPath;
                        _logger.LogInformation("Imported PDF found at: {FullPath}", fullPath);
                    }
                    else
                    {
                        _logger.LogWarning("Imported PDF was not found on disk: {TempPath}", jobParams.TempPdfPath);
                    }
                }

                _logger.LogInformation("Source content built: {Chars} chars from lectures. PDF attached: {HasPdf}",
                    sourceContent.Length, validPdfPath != null);

                var questionTypes = jobParams.QuestionTypes ?? "MCQ";

                // Build the prompt differently based on whether a PDF is attached
                string prompt;
                if (!string.IsNullOrEmpty(validPdfPath))
                {
                    // When a PDF is attached, Gemini can read it directly — instruct it clearly
                    var extraContext = sourceContent.Length > 10
                        ? $"\n\nAdditional context from lectures:\n{sourceContent}"
                        : "";

                    prompt = $@"You are an educational quiz generator. Read the attached PDF document carefully and generate exactly {jobParams.NumberOfQuestions} quiz questions STRICTLY based on the content of that PDF.

Question types required: {questionTypes}
Difficulty level: {jobParams.DifficultyLevel}
{extraContext}
{(string.IsNullOrEmpty(jobParams.CustomPrompt) ? "" : $"Additional instructions: {jobParams.CustomPrompt}")}

IMPORTANT: Generate questions ONLY from the topics and content found in the attached PDF. Do NOT use general knowledge or unrelated topics.

For each question provide:
1. questionText - The question text in English
2. questionType - One of: MCQ, Written, TrueFalse
3. marks - Points (1-5 based on difficulty)
4. difficultyLevel - Easy, Medium, or Hard
5. explanation - Why the correct answer is correct, referencing the source material
6. sourceReference - The specific topic, section, or page from the PDF
7. options - For MCQ: exactly 4 options. For TrueFalse: True and False. Each with text and isCorrect boolean. For Written: empty array.

Return ONLY a valid JSON array with this exact schema, no markdown formatting:
[{{
  ""questionText"": ""..."",
  ""questionType"": ""MCQ"",
  ""marks"": 1,
  ""difficultyLevel"": ""{jobParams.DifficultyLevel}"",
  ""explanation"": ""..."",
  ""sourceReference"": ""..."",
  ""options"": [
    {{ ""text"": ""..."", ""isCorrect"": false }},
    {{ ""text"": ""..."", ""isCorrect"": true }},
    {{ ""text"": ""..."", ""isCorrect"": false }},
    {{ ""text"": ""..."", ""isCorrect"": false }}
  ]
}}]";
                }
                else
                {
                    // Text-only prompt
                    prompt = $@"You are an educational quiz generator. Based on the following educational content, generate exactly {jobParams.NumberOfQuestions} quiz questions STRICTLY from the provided material.

Question types required: {questionTypes}
Difficulty level: {jobParams.DifficultyLevel}

Source material:
{sourceContent}

{(string.IsNullOrEmpty(jobParams.CustomPrompt) ? "" : $"Additional instructions: {jobParams.CustomPrompt}")}

IMPORTANT: Generate questions ONLY from the topics and content provided above.

For each question provide:
1. questionText - The question text in English
2. questionType - One of: MCQ, Written, TrueFalse
3. marks - Points (1-5 based on difficulty)
4. difficultyLevel - Easy, Medium, or Hard
5. explanation - Why the correct answer is correct, referencing the source
6. sourceReference - The specific topic or section from the source material
7. options - For MCQ: exactly 4 options. For TrueFalse: True and False. Each with text and isCorrect boolean. For Written: empty array.

Return ONLY a valid JSON array with this exact schema, no markdown formatting:
[{{
  ""questionText"": ""..."",
  ""questionType"": ""MCQ"",
  ""marks"": 1,
  ""difficultyLevel"": ""{jobParams.DifficultyLevel}"",
  ""explanation"": ""..."",
  ""sourceReference"": ""..."",
  ""options"": [
    {{ ""text"": ""..."", ""isCorrect"": false }},
    {{ ""text"": ""..."", ""isCorrect"": true }},
    {{ ""text"": ""..."", ""isCorrect"": false }},
    {{ ""text"": ""..."", ""isCorrect"": false }}
  ]
}}]";
                }

                string aiResponse;
                try 
                {
                    if (!string.IsNullOrEmpty(validPdfPath))
                    {
                        aiResponse = await _geminiService.GenerateQuizQuestionsWithFileAsync(prompt, validPdfPath, "Pdf");
                    }
                    else
                    {
                        aiResponse = await _geminiService.GenerateQuizQuestionsAsync(prompt);
                    }
                }
                finally
                {
                    if (!string.IsNullOrEmpty(validPdfPath) && File.Exists(validPdfPath))
                    {
                        File.Delete(validPdfPath);
                    }
                }

                // Parse AI response
                var cleanJson = aiResponse.Trim();

                var startIdx = cleanJson.IndexOf('[');
                var endIdx   = cleanJson.LastIndexOf(']');
                if (startIdx >= 0 && endIdx > startIdx)
                    cleanJson = cleanJson.Substring(startIdx, endIdx - startIdx + 1);

                // Safety: recover partial truncated JSON
                if (!cleanJson.EndsWith("]"))
                {
                    _logger.LogWarning("AiQuizGeneratorJob: AI response JSON truncated. Attempting partial recovery.");
                    var lastBrace = cleanJson.LastIndexOf('}');
                    if (lastBrace > 0)
                        cleanJson = cleanJson.Substring(0, lastBrace + 1) + "]";
                    else
                        throw new Exception("AI response JSON is too truncated to recover.");
                }

                var generatedQuestions = JsonSerializer.Deserialize<List<AiGeneratedQuestion>>(cleanJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new Exception("Failed to parse AI-generated questions.");

                // Add questions to the quiz
                int sortOrder = 0;
                foreach (var gq in generatedQuestions)
                {
                    var question = new Question
                    {
                        QuestionText = gq.QuestionText,
                        QuestionType = gq.QuestionType,
                        Marks = gq.Marks,
                        DifficultyLevel = gq.DifficultyLevel,
                        Explanation = gq.Explanation,
                        SourceReference = gq.SourceReference,
                        SortOrder = sortOrder++
                    };

                    if (gq.Options != null)
                    {
                        int optSort = 0;
                        foreach (var opt in gq.Options)
                        {
                            question.Options.Add(new QuestionOption
                            {
                                OptionText = opt.Text,
                                IsCorrect = opt.IsCorrect,
                                SortOrder = optSort++
                            });
                        }
                    }

                    quiz.Questions.Add(question);
                    quiz.TotalMarks += gq.Marks;
                }

                // Mark quiz as generated (still NOT visible to students)
                quiz.Description = "AI-Generated Quiz based on provided materials. Please review and make visible.";
                quiz.AiPromptUsed = jobParams.CustomPrompt; // Store original custom prompt, not job params
                quiz.IsVisible = false; // Admin/Instructor must manually make it visible

                await _context.SaveChangesAsync();

                _logger.LogInformation("AiQuizGeneratorJob completed. QuizId: {QuizId}, Questions: {Count}", quizId, generatedQuestions.Count);

                await _activityService.TrackEntityActionAsync(ActivityActions.QuizGeneratedByAI, "Quiz", quiz.Id, userId,
                    $"AI-generated quiz '{quiz.Title}' with {generatedQuestions.Count} questions");

                // --- SUCCESS NOTIFICATIONS ---
                var courseName = quiz.Course?.Title ?? "your course";
                var notifTitle = "✅ AI Quiz Generated Successfully";
                var notifBody = $"Your AI quiz '{quiz.Title}' ({generatedQuestions.Count} questions) in '{courseName}' has been generated and is ready for review.\n\n⚠️ The quiz is currently HIDDEN from students. Please review it and make it visible when ready.";

                // 1. In-app notification (database)
                await _notificationService.CreateNotificationAsync(userId, notifTitle, notifBody, "AIQuizReady", quizId);

                // 2. SignalR real-time push
                await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notifBody);

                // 3. Email notification
                if (creator != null && !string.IsNullOrEmpty(creator.Email))
                {
                    try
                    {
                        var htmlBody = $@"
                        <div style='font-family: Segoe UI, sans-serif; max-width: 600px; margin: 0 auto; background: #f4f7f6; padding: 20px;'>
                            <div style='background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                                <div style='background: linear-gradient(135deg, #1a2744 0%, #2c3e6b 50%, #c9a84c 100%); padding: 30px; text-align: center; color: white;'>
                                    <div style='font-size: 40px; margin-bottom: 10px;'>🤖✅</div>
                                    <h1 style='margin: 0; font-size: 24px;'>AI Quiz Ready!</h1>
                                    <p style='margin: 8px 0 0 0; color: #e2d5a0;'>SkyLearn Platform</p>
                                </div>
                                <div style='padding: 30px;'>
                                    <p style='color: #374151; font-size: 16px;'>Hello <strong>{creator.FullName}</strong>,</p>
                                    <p style='color: #4b5563; line-height: 1.6;'>
                                        Your AI-generated quiz has been successfully created and is now ready for your review.
                                    </p>
                                    <div style='background: #eff6ff; padding: 20px; border-radius: 8px; border-left: 4px solid #3b82f6; margin: 20px 0;'>
                                        <p style='margin: 0; color: #1e40af;'><strong>📝 Quiz:</strong> {quiz.Title}</p>
                                        <p style='margin: 8px 0 0 0; color: #1e40af;'><strong>📚 Course:</strong> {courseName}</p>
                                        <p style='margin: 8px 0 0 0; color: #1e40af;'><strong>❓ Questions:</strong> {generatedQuestions.Count}</p>
                                        <p style='margin: 8px 0 0 0; color: #1e40af;'><strong>⭐ Total Marks:</strong> {quiz.TotalMarks}</p>
                                    </div>
                                    <div style='background: #fef9c3; padding: 15px; border-radius: 8px; border-left: 4px solid #eab308; margin: 20px 0;'>
                                        <p style='margin: 0; color: #92400e;'>
                                            ⚠️ <strong>Action Required:</strong> The quiz is currently <strong>hidden</strong> from students.
                                            Please log in, review the quiz, and make it visible when you're ready.
                                        </p>
                                    </div>
                                </div>
                                <div style='text-align: center; padding: 20px; background: #1a2744;'>
                                    <p style='margin: 0; color: #8b9dc3; font-size: 12px;'>© 2026 SkyLearn Platform — Egypt Air Force College</p>
                                </div>
                            </div>
                        </div>";

                        await _emailService.SendEmailAsync(creator.Email, $"✅ SkyLearn: AI Quiz '{quiz.Title}' is Ready for Review", htmlBody);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "AiQuizGeneratorJob: Failed to send completion email to {Email}", creator.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AiQuizGeneratorJob FAILED for QuizId: {QuizId}", quizId);

                // Mark the quiz as failed by updating its description
                if (quiz != null)
                {
                    quiz.Description = $"[AI Generation Failed] {ex.Message}";
                    quiz.IsVisible = false;
                    quiz.AiPromptUsed = null;
                    await _context.SaveChangesAsync();
                }

                // Notify creator of failure
                var failTitle = "❌ AI Quiz Generation Failed";
                var failBody = $"Failed to generate quiz '{quiz?.Title}'. Error: {ex.Message.Substring(0, Math.Min(200, ex.Message.Length))}";
                await _notificationService.CreateNotificationAsync(userId, failTitle, failBody, "System");
                await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", failBody);
            }
        }
    }

    /// <summary>Serialized parameters stored temporarily in AiPromptUsed field while job is pending.</summary>
    public class AiQuizJobParams
    {
        public List<int>? LectureIds { get; set; }
        public string? TempPdfPath { get; set; }
        public string? QuestionTypes { get; set; }
        public int NumberOfQuestions { get; set; } = 10;
        public string DifficultyLevel { get; set; } = "Medium";
        public string? CustomPrompt { get; set; }
    }
}
