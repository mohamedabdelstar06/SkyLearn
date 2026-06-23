using SkyLearnApi.DTOs.Quizzes;
using Hangfire;

namespace SkyLearnApi.Services.Implementation
{
    public class QuizService : IQuizService
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly IActivityService _activityService;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<QuizService> _logger;
        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;

        public QuizService(AppDbContext context, IGeminiService geminiService, IActivityService activityService,
            INotificationService notificationService, IWebHostEnvironment env, ILogger<QuizService> logger,
            Hangfire.IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _geminiService = geminiService;
            _activityService = activityService;
            _notificationService = notificationService;
            _env = env;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<QuizResponseDto> CreateAsync(int courseId, CreateQuizDto dto, int userId)
        {
            _logger.LogInformation("Creating quiz in course {CourseId}. Title: {Title}, Questions: {QuestionCount}, UserId: {UserId}",
                courseId, dto.Title, dto.Questions?.Count ?? 0, userId);

            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException($"Course with ID {courseId} not found.");

            var quiz = new Quiz
            {
                CourseId = courseId,
                Title = dto.Title,
                Description = dto.Description,
                TimeLimitMinutes = dto.TimeLimitMinutes,
                MaxAttempts = dto.MaxAttempts,
                PassingScore = dto.PassingScore,
                ShuffleQuestions = dto.ShuffleQuestions,
                ShuffleAnswers = dto.ShuffleAnswers,
                ShowCorrectAnswers = dto.ShowCorrectAnswers,
                ShowExplanations = dto.ShowExplanations,
                GradingMode = dto.GradingMode,
                StartDate = dto.StartDate,
                DeadLineDate = dto.DeadLineDate,
                TargetSquadronId = dto.TargetSquadronId,
                DifficultyLevel = dto.DifficultyLevel,
                SortOrder = dto.SortOrder,
                IsVisible = dto.IsVisible,
                QuizScope = "Course",
                CreatedById = userId,
                TotalMarks = 0
            };

            if (dto.Questions != null)
            {
                foreach (var qDto in dto.Questions)
                {
                    var question = new Question
                    {
                        QuestionText = qDto.QuestionText,
                        QuestionType = qDto.QuestionType,
                        Marks = qDto.Marks,
                        DifficultyLevel = qDto.DifficultyLevel,
                        Explanation = qDto.Explanation,
                        SourceReference = qDto.SourceReference,
                        SortOrder = qDto.SortOrder
                    };

                    if (qDto.Options != null)
                    {
                        foreach (var oDto in qDto.Options)
                        {
                            question.Options.Add(new QuestionOption
                            {
                                OptionText = oDto.OptionText,
                                IsCorrect = oDto.IsCorrect,
                                SortOrder = oDto.SortOrder
                            });
                        }
                    }

                    quiz.Questions.Add(question);
                    quiz.TotalMarks += qDto.Marks;
                }
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            
            // Fetch the user so the CreatedByName is populated in the response
            var createdByUser = await _context.Users.FindAsync(userId);
            quiz.CreatedBy = createdByUser!;

            _logger.LogInformation("Quiz created successfully. QuizId: {QuizId}, CourseId: {CourseId}, Title: {Title}, TotalMarks: {TotalMarks}",
                quiz.Id, courseId, quiz.Title, quiz.TotalMarks);

            await _activityService.TrackEntityActionAsync(ActivityActions.QuizCreated, "Quiz", quiz.Id, userId,
                $"Quiz '{quiz.Title}' created in course '{course.Title}'");

            // Notify enrolled students about new quiz
            await _notificationService.NotifyEnrolledStudentsAsync(courseId,
                "New Quiz Available",
                $"A new quiz '{quiz.Title}' has been added to '{course.Title}'." +
                (dto.DeadLineDate.HasValue ? $" DeadLine: {dto.DeadLineDate.Value:g}" : ""),
                "NewQuiz", quiz.Id);

            // Notify the creator
            await _notificationService.CreateNotificationAsync(userId,
                "Quiz Created Successfully",
                $"You have successfully created the quiz '{quiz.Title}' in '{course.Title}'.",
                "System", quiz.Id);

            return MapToResponseDto(quiz, course.Title);
        }

        public async Task<List<QuizResponseDto>> GetByCourseAsync(int courseId, int userId, string userRole)
        {
            var course = await _context.Courses.FindAsync(courseId);
            var query = _context.Quizzes
                .Where(q => q.CourseId == courseId)
                .Include(q => q.CreatedBy)
                .Include(q => q.TargetSquadron)
                .Include(q => q.Questions) // Include Questions only for QuestionCount - no Options loaded
                .OrderBy(q => q.SortOrder)
                .AsQueryable();

            if (userRole == Roles.Student)
                query = query.Where(q => q.IsVisible && q.QuizScope == "Course");

            var quizzes = await query.ToListAsync();
            // Pass false to exclude question details - only question count is returned
            return quizzes.Select(q => MapToResponseDto(q, course?.Title ?? "", false)).ToList();
        }

        public async Task<QuizResponseDto?> GetByIdAsync(int id, int userId, string userRole)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.CreatedBy)
                .Include(q => q.TargetSquadron)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return null;
            if (userRole == Roles.Student && (!quiz.IsVisible || quiz.QuizScope != "Course")) return null;

            return MapToResponseDto(quiz, quiz.Course?.Title ?? "");
        }

        public async Task<QuizResponseDto?> UpdateAsync(int id, UpdateQuizDto dto, int userId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.CreatedBy)
                .Include(q => q.TargetSquadron)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (quiz == null) return null;

            if (dto.Title != null) quiz.Title = dto.Title;
            if (dto.Description != null) quiz.Description = dto.Description;
            if (dto.TimeLimitMinutes.HasValue) quiz.TimeLimitMinutes = dto.TimeLimitMinutes;
            if (dto.MaxAttempts.HasValue) quiz.MaxAttempts = dto.MaxAttempts.Value;
            if (dto.PassingScore.HasValue) quiz.PassingScore = dto.PassingScore;
            if (dto.ShuffleQuestions.HasValue) quiz.ShuffleQuestions = dto.ShuffleQuestions.Value;
            if (dto.ShuffleAnswers.HasValue) quiz.ShuffleAnswers = dto.ShuffleAnswers.Value;
            if (dto.ShowCorrectAnswers.HasValue) quiz.ShowCorrectAnswers = dto.ShowCorrectAnswers.Value;
            if (dto.ShowExplanations.HasValue) quiz.ShowExplanations = dto.ShowExplanations.Value;
            if (dto.GradingMode != null) quiz.GradingMode = dto.GradingMode;
            if (dto.StartDate.HasValue) quiz.StartDate = dto.StartDate;
            if (dto.DeadLineDate.HasValue) quiz.DeadLineDate = dto.DeadLineDate;
            if (dto.TargetSquadronId.HasValue) quiz.TargetSquadronId = dto.TargetSquadronId;
            if (dto.DifficultyLevel != null) quiz.DifficultyLevel = dto.DifficultyLevel;
            if (dto.SortOrder.HasValue) quiz.SortOrder = dto.SortOrder.Value;
            if (dto.IsVisible.HasValue) quiz.IsVisible = dto.IsVisible.Value;

            if (dto.Questions != null)
            {
                var matchedQuestions = new Dictionary<UpdateQuizQuestionDto, Question>();
                var usedExistingQuestionIds = new HashSet<int>();

                foreach (var qDto in dto.Questions)
                {
                    Question? existingQ = null;
                    if (qDto.Id.HasValue && qDto.Id.Value > 0)
                    {
                        existingQ = quiz.Questions.FirstOrDefault(q => q.Id == qDto.Id.Value);
                    }
                    
                    if (existingQ == null)
                    {
                        existingQ = quiz.Questions.FirstOrDefault(q => q.SortOrder == qDto.SortOrder && !usedExistingQuestionIds.Contains(q.Id));
                    }

                    if (existingQ != null)
                    {
                        matchedQuestions[qDto] = existingQ;
                        usedExistingQuestionIds.Add(existingQ.Id);
                    }
                }

                var questionsToRemove = quiz.Questions.Where(q => !usedExistingQuestionIds.Contains(q.Id)).ToList();
                
                foreach(var qRemove in questionsToRemove)
                {
                    quiz.TotalMarks -= qRemove.Marks;
                    _context.Questions.Remove(qRemove);
                }

                foreach (var qDto in dto.Questions)
                {
                    if (matchedQuestions.TryGetValue(qDto, out var existingQ))
                    {
                        quiz.TotalMarks = quiz.TotalMarks - existingQ.Marks + qDto.Marks;
                        
                        existingQ.QuestionText = qDto.QuestionText;
                        existingQ.QuestionType = qDto.QuestionType;
                        existingQ.Marks = qDto.Marks;
                        if (qDto.DifficultyLevel != null) existingQ.DifficultyLevel = qDto.DifficultyLevel;
                        existingQ.Explanation = qDto.Explanation;
                        existingQ.SourceReference = qDto.SourceReference;
                        existingQ.SortOrder = qDto.SortOrder;

                        if (qDto.Options != null)
                        {
                            var matchedOptions = new Dictionary<UpdateQuizOptionDto, QuestionOption>();
                            var usedOptionIds = new HashSet<int>();

                            foreach (var oDto in qDto.Options)
                            {
                                QuestionOption? existingO = null;
                                if (oDto.Id.HasValue && oDto.Id.Value > 0)
                                {
                                    existingO = existingQ.Options.FirstOrDefault(o => o.Id == oDto.Id.Value);
                                }
                                if (existingO == null)
                                {
                                    existingO = existingQ.Options.FirstOrDefault(o => o.SortOrder == oDto.SortOrder && !usedOptionIds.Contains(o.Id));
                                }

                                if (existingO != null)
                                {
                                    matchedOptions[oDto] = existingO;
                                    usedOptionIds.Add(existingO.Id);
                                }
                            }

                            var optionsToRemove = existingQ.Options.Where(o => !usedOptionIds.Contains(o.Id)).ToList();
                            _context.QuestionOptions.RemoveRange(optionsToRemove);

                            foreach(var oDto in qDto.Options)
                            {
                                if (matchedOptions.TryGetValue(oDto, out var existingO))
                                {
                                    existingO.OptionText = oDto.OptionText;
                                    existingO.IsCorrect = oDto.IsCorrect;
                                    existingO.SortOrder = oDto.SortOrder;
                                }
                                else
                                {
                                    existingQ.Options.Add(new QuestionOption
                                    {
                                        OptionText = oDto.OptionText,
                                        IsCorrect = oDto.IsCorrect,
                                        SortOrder = oDto.SortOrder
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        var newQuestion = new Question
                        {
                            QuestionText = qDto.QuestionText,
                            QuestionType = qDto.QuestionType,
                            Marks = qDto.Marks,
                            DifficultyLevel = qDto.DifficultyLevel ?? "Medium",
                            Explanation = qDto.Explanation,
                            SourceReference = qDto.SourceReference,
                            SortOrder = qDto.SortOrder
                        };

                        if (qDto.Options != null)
                        {
                            foreach (var oDto in qDto.Options)
                            {
                                newQuestion.Options.Add(new QuestionOption
                                {
                                    OptionText = oDto.OptionText,
                                    IsCorrect = oDto.IsCorrect,
                                    SortOrder = oDto.SortOrder
                                });
                            }
                        }

                        quiz.Questions.Add(newQuestion);
                        quiz.TotalMarks += qDto.Marks;
                    }
                }
            }

            quiz.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.QuizUpdated, "Quiz", quiz.Id, userId,
                $"Quiz '{quiz.Title}' updated");

            return MapToResponseDto(quiz, quiz.Course?.Title ?? "");
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return false;

            var quizTitle = quiz.Title;
            var courseId = quiz.CourseId;

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.QuizDeleted, "Quiz", id, userId,
                $"Quiz '{quizTitle}' deleted from course {courseId}");

            return true;
        }

        public async Task<object> GenerateWithAiAsync(GenerateQuizDto dto, int userId)
        {
            // Validate course
            var courseId = dto.CourseId ?? (dto.LectureIds?.Any() == true
                ? await _context.Lectures.Where(l => l.Id == dto.LectureIds.First()).Select(l => l.CourseId).FirstOrDefaultAsync()
                : 0);

            if (courseId == 0)
                throw new ArgumentException("CourseId is required when not selecting lectures.");

            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException("Course not found.");

            // Handle imported PDF: save to temp storage so the background job can read it
            string? tempPdfPath = null;
            if (dto.ImportedPdf != null)
                tempPdfPath = await FileHelper.SaveFileAsync(dto.ImportedPdf, "temp", _env);

            // Serialize generation parameters into the quiz shell
            var jobParams = new AiQuizJobParams
            {
                LectureIds = dto.LectureIds,
                TempPdfPath = tempPdfPath,
                QuestionTypes = dto.QuestionTypes,
                NumberOfQuestions = dto.NumberOfQuestions,
                DifficultyLevel = dto.DifficultyLevel,
                CustomPrompt = dto.CustomPrompt
            };

            // Create a pending quiz shell (IsVisible = false, no questions yet)
            var quiz = new Quiz
            {
                CourseId = courseId,
                Title = dto.Title ?? $"AI Quiz - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                Description = $"⏳ AI is generating {dto.NumberOfQuestions} questions. Please wait...",
                IsAiGenerated = true,
                AiPromptUsed = JsonSerializer.Serialize(jobParams), // temp: store job params
                DifficultyLevel = dto.DifficultyLevel,
                QuizScope = dto.QuizScope,
                TargetSquadronId = dto.TargetSquadronId,
                GradingMode = (dto.QuestionTypes ?? "").Contains("Written") ? "Mixed" : "Auto",
                SourceLectureIds = dto.LectureIds != null ? JsonSerializer.Serialize(dto.LectureIds) : null,
                CreatedById = userId,
                TotalMarks = 0,
                IsVisible = false, // Hidden from students until admin reviews & publishes
                ShowCorrectAnswers = true,
                ShowExplanations = true
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            _logger.LogInformation("AI Quiz shell created. QuizId: {QuizId}. Enqueueing background job.", quiz.Id);

            // Enqueue background job (no HTTP timeout risk)
            _backgroundJobClient.Enqueue<AiQuizGeneratorJob>(j => j.ProcessGenerateAsync(quiz.Id, userId));

            // Return immediate response to client
            return new
            {
                quizId = quiz.Id,
                title = quiz.Title,
                status = "Pending",
                message = $"⏳ AI is generating your quiz '{quiz.Title}' in the background. You will receive an in-app notification and email when it's ready. The quiz will be hidden from students until you manually make it visible.",
                estimatedMinutes = 2
            };
        }

        public async Task<QuizTakeResponseDto> TakeQuizAsync(int quizId, int studentId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions.OrderBy(qu => qu.SortOrder))
                    .ThenInclude(q => q.Options.OrderBy(o => o.SortOrder))
                .FirstOrDefaultAsync(q => q.Id == quizId)
                ?? throw new ArgumentException("Quiz not found.");

            // Check attempt count
            var existingAttempts = await _context.QuizAttempts
                .CountAsync(a => a.QuizId == quizId && a.StudentId == studentId);

            if (existingAttempts >= quiz.MaxAttempts)
                throw new ArgumentException($"Maximum attempts ({quiz.MaxAttempts}) reached.");

            // Check for in-progress attempt
            var inProgress = await _context.QuizAttempts
                .FirstOrDefaultAsync(a => a.QuizId == quizId && a.StudentId == studentId && a.Status == "InProgress");

            if (inProgress != null)
            {
                // Resume existing attempt
                return new QuizTakeResponseDto
                {
                    Id = quiz.Id,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    TimeLimitMinutes = quiz.TimeLimitMinutes,
                    TotalMarks = quiz.TotalMarks,
                    AttemptNumber = inProgress.AttemptNumber,
                    Questions = quiz.Questions.Select(q => new QuestionTakeDto
                    {
                        Id = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionTextAr = q.QuestionTextAr,
                        QuestionType = q.QuestionType,
                        Marks = q.Marks,
                        SortOrder = q.SortOrder,
                        ImageUrl = q.ImageUrl,
                        Options = q.Options.Select(o => new OptionTakeDto
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            OptionTextAr = o.OptionTextAr,
                            SortOrder = o.SortOrder
                        }).ToList()
                    }).ToList()
                };
            }

            // Create new attempt
            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                StudentId = studentId,
                AttemptNumber = existingAttempts + 1,
                MaxScore = quiz.TotalMarks,
                Status = "InProgress"
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.QuizStarted, "Quiz", quizId, studentId,
                $"Student started quiz '{quiz.Title}' - Attempt {attempt.AttemptNumber}");

            var questions = quiz.Questions.ToList();
            if (quiz.ShuffleQuestions)
            {
                var rng = new Random();
                questions = questions.OrderBy(_ => rng.Next()).ToList();
            }

            return new QuizTakeResponseDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                TotalMarks = quiz.TotalMarks,
                AttemptNumber = attempt.AttemptNumber,
                Questions = questions.Select(q => new QuestionTakeDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionTextAr = q.QuestionTextAr,
                    QuestionType = q.QuestionType,
                    Marks = q.Marks,
                    SortOrder = q.SortOrder,
                    ImageUrl = q.ImageUrl,
                    Options = q.Options.Select(o => new OptionTakeDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        OptionTextAr = o.OptionTextAr,
                        SortOrder = o.SortOrder
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<QuizResultResponseDto> SubmitQuizAsync(int quizId, SubmitQuizDto dto, int studentId)
        {
            var attempt = await _context.QuizAttempts
                .FirstOrDefaultAsync(a => a.QuizId == quizId && a.StudentId == studentId && a.Status == "InProgress")
                ?? throw new ArgumentException("No in-progress attempt found for this quiz.");

            var quiz = await _context.Quizzes
                .Include(q => q.Questions).ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId)
                ?? throw new ArgumentException("Quiz not found.");

            if (quiz.TimeLimitMinutes.HasValue)
            {
                var deadline = attempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes.Value).AddMinutes(1); // 1 min buffer
                if (DateTime.UtcNow > deadline)
                {
                    throw new InvalidOperationException("Time limit exceeded. The quiz attempt has expired and cannot be submitted.");
                }
            }

            decimal totalScore = 0;
            bool hasWritten = false;

            foreach (var answerDto in dto.Answers)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
                if (question == null) continue;

                var studentAnswer = new StudentAnswer
                {
                    QuizAttemptId = attempt.Id,
                    QuestionId = answerDto.QuestionId,
                    SelectedOptionId = answerDto.SelectedOptionId,
                    WrittenAnswer = answerDto.WrittenAnswer
                };

                // Auto-grade MCQ and TrueFalse
                if (question.QuestionType is "MCQ" or "TrueFalse" && answerDto.SelectedOptionId.HasValue)
                {
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                    studentAnswer.IsCorrect = correctOption?.Id == answerDto.SelectedOptionId;
                    studentAnswer.MarksAwarded = studentAnswer.IsCorrect == true ? question.Marks : 0;
                    totalScore += studentAnswer.MarksAwarded ?? 0;
                }
                else if (question.QuestionType == "Written")
                {
                    hasWritten = true;
                }

                _context.StudentAnswers.Add(studentAnswer);
            }

            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.TimeSpentSeconds = (int)(attempt.SubmittedAt.Value - attempt.StartedAt).TotalSeconds;
            attempt.Score = totalScore;
            attempt.ScorePercent = attempt.MaxScore > 0 ? Math.Round((totalScore / attempt.MaxScore) * 100, 2) : 0;

            if (hasWritten)
            {
                attempt.Status = "Submitted";
                attempt.IsGraded = false;
            }
            else
            {
                attempt.Status = "Graded";
                attempt.IsGraded = true;
                attempt.GradedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.QuizSubmitted, "Quiz", quizId, studentId,
                $"Quiz submitted - Score: {totalScore}/{attempt.MaxScore}",
                metadata: new { score = totalScore, maxScore = attempt.MaxScore, timeSpent = attempt.TimeSpentSeconds });

            var student = await _context.Users.FindAsync(studentId);

            return new QuizResultResponseDto
            {
                AttemptId = attempt.Id,
                QuizId = quizId,
                QuizTitle = quiz.Title,
                StudentId = studentId,
                StudentName = student?.FullName ?? "",
                AttemptNumber = attempt.AttemptNumber,
                Score = attempt.Score,
                MaxScore = attempt.MaxScore,
                ScorePercent = attempt.ScorePercent,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                TimeSpentSeconds = attempt.TimeSpentSeconds
            };
        }

        public async Task<QuizStudentResultsDto> GetQuizResultsAsync(int quizId, int userId, string userRole)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId)
                ?? throw new ArgumentException("Quiz not found.");

            var attempts = await _context.QuizAttempts
                .Where(a => a.QuizId == quizId)
                .Include(a => a.Student)
                .Include(a => a.Answers).ThenInclude(sa => sa.Question)
                .Include(a => a.Answers).ThenInclude(sa => sa.SelectedOption)
                .OrderBy(a => a.StudentId).ThenBy(a => a.AttemptNumber)
                .ToListAsync();

            return new QuizStudentResultsDto
            {
                QuizId = quizId,
                QuizTitle = quiz.Title,
                TotalMarks = quiz.TotalMarks,
                Results = attempts.Select(a => MapAttemptToResult(a, quiz)).ToList()
            };
        }

        public async Task<QuizResultResponseDto?> GetMyResultAsync(int quizId, int studentId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions).ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return null;

            var attempt = await _context.QuizAttempts
                .Where(a => a.QuizId == quizId && a.StudentId == studentId)
                .Include(a => a.Student)
                .Include(a => a.Answers).ThenInclude(sa => sa.Question).ThenInclude(q => q.Options)
                .Include(a => a.Answers).ThenInclude(sa => sa.SelectedOption)
                .OrderByDescending(a => a.AttemptNumber)
                .FirstOrDefaultAsync();

            if (attempt == null) return null;

            var result = MapAttemptToResult(attempt, quiz);
            if (quiz.ShowCorrectAnswers && (attempt.Status == "Graded" || attempt.Status == "Submitted"))
            {
                result.Answers = attempt.Answers.Select(sa =>
                {
                    var q = sa.Question;
                    var correctOpt = q.Options.FirstOrDefault(o => o.IsCorrect);
                    return new StudentAnswerResponseDto
                    {
                        Id = sa.Id,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Marks = q.Marks,
                        SelectedOptionText = sa.SelectedOption?.OptionText,
                        CorrectOptionText = correctOpt?.OptionText,
                        WrittenAnswer = sa.WrittenAnswer,
                        IsCorrect = sa.IsCorrect,
                        MarksAwarded = sa.MarksAwarded,
                        InstructorFeedback = sa.InstructorFeedback,
                        Explanation = quiz.ShowExplanations ? q.Explanation : null,
                        ExplanationAr = quiz.ShowExplanations ? q.ExplanationAr : null,
                        SourceReference = q.SourceReference
                    };
                }).ToList();
            }

            return result;
        }

        public async Task<QuizResultResponseDto> GradeQuizAsync(int quizId, GradeQuizDto dto, int graderId)
        {
            var attempt = await _context.QuizAttempts
                .Where(a => a.QuizId == quizId && a.Status == "Submitted")
                .Include(a => a.Answers)
                .Include(a => a.Student)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException("No submitted attempt to grade.");

            foreach (var grade in dto.Grades)
            {
                var answer = attempt.Answers.FirstOrDefault(a => a.Id == grade.StudentAnswerId);
                if (answer == null) continue;

                answer.MarksAwarded = grade.MarksAwarded;
                answer.InstructorFeedback = grade.Feedback;
                answer.IsCorrect = grade.MarksAwarded > 0;
            }

            attempt.Score = attempt.Answers.Sum(a => a.MarksAwarded ?? 0);
            attempt.ScorePercent = attempt.MaxScore > 0 ? Math.Round((attempt.Score.Value / attempt.MaxScore) * 100, 2) : 0;
            attempt.Status = "Graded";
            attempt.IsGraded = true;
            attempt.GradedById = graderId;
            attempt.GradedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.QuizGraded, "Quiz", quizId, graderId,
                $"Quiz graded - Score: {attempt.Score}/{attempt.MaxScore} for student {attempt.Student?.FullName}",
                metadata: new { studentId = attempt.StudentId, score = attempt.Score, maxScore = attempt.MaxScore });

            await _notificationService.CreateNotificationAsync(attempt.StudentId,
                "Quiz Graded", $"Your quiz has been graded. Score: {attempt.Score}/{attempt.MaxScore}",
                "GradePublished", quizId);

            var quiz = await _context.Quizzes.FindAsync(quizId)!;
            return MapAttemptToResult(attempt, quiz!);
        }

        public async Task<bool> AutoSaveProgressAsync(int quizId, QuizAutoSaveDto dto, int studentId)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.QuizId == quizId && a.StudentId == studentId && a.Status == "InProgress");

            if (attempt == null)
                return false;

            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz != null && quiz.TimeLimitMinutes.HasValue)
            {
                var deadline = attempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes.Value).AddMinutes(1); // 1 min buffer
                if (DateTime.UtcNow > deadline)
                {
                    // If time is exceeded, we can stop saving and optionally auto-submit
                    return false; 
                }
            }

            foreach (var answerDto in dto.Answers)
            {
                var existingAnswer = attempt.Answers.FirstOrDefault(a => a.QuestionId == answerDto.QuestionId);
                
                if (existingAnswer != null)
                {
                    existingAnswer.SelectedOptionId = answerDto.SelectedOptionId;
                    existingAnswer.WrittenAnswer = answerDto.WrittenAnswer;
                    existingAnswer.IsFlagged = answerDto.IsFlagged;
                }
                else
                {
                    _context.StudentAnswers.Add(new StudentAnswer
                    {
                        QuizAttemptId = attempt.Id,
                        QuestionId = answerDto.QuestionId,
                        SelectedOptionId = answerDto.SelectedOptionId,
                        WrittenAnswer = answerDto.WrittenAnswer,
                        IsFlagged = answerDto.IsFlagged
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task TranslateQuizAsync(int quizId)
        {
            var questions = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .Include(q => q.Options)
                .ToListAsync();

            if (!questions.Any()) throw new ArgumentException("No questions to translate.");

            // Build content for batch translation
            var contentToTranslate = new StringBuilder();
            contentToTranslate.AppendLine("Translate each item below from English to Arabic. Return a JSON array of objects, each with 'index', 'questionTextAr', and 'options' (array of 'optionTextAr'). Keep technical terms in English where appropriate.");
            contentToTranslate.AppendLine("[");
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                contentToTranslate.AppendLine($"{{ \"index\": {i}, \"questionText\": \"{EscapeJson(q.QuestionText)}\", \"explanation\": \"{EscapeJson(q.Explanation ?? "")}\", \"options\": [{string.Join(",", q.Options.Select(o => $"\"{EscapeJson(o.OptionText)}\""))}] }}{(i < questions.Count - 1 ? "," : "")}");
            }
            contentToTranslate.AppendLine("]");

            var aiResponse = await _geminiService.TranslateToArabicAsync(contentToTranslate.ToString());

            try
            {
                var cleanJson = aiResponse.Trim();
                if (cleanJson.StartsWith("```"))
                {
                    cleanJson = cleanJson.Substring(cleanJson.IndexOf('['));
                    cleanJson = cleanJson.Substring(0, cleanJson.LastIndexOf(']') + 1);
                }

                using var doc = JsonDocument.Parse(cleanJson);
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var index = item.GetProperty("index").GetInt32();
                    if (index >= questions.Count) continue;

                    var q = questions[index];
                    if (item.TryGetProperty("questionTextAr", out var qTextAr))
                        q.QuestionTextAr = qTextAr.GetString();
                    if (item.TryGetProperty("explanationAr", out var expAr))
                        q.ExplanationAr = expAr.GetString();

                    if (item.TryGetProperty("options", out var opts))
                    {
                        var optArray = opts.EnumerateArray().ToList();
                        for (int j = 0; j < Math.Min(optArray.Count, q.Options.Count); j++)
                        {
                            var option = q.Options.ElementAt(j);
                            if (optArray[j].ValueKind == JsonValueKind.Object && optArray[j].TryGetProperty("optionTextAr", out var optText))
                                option.OptionTextAr = optText.GetString();
                            else if (optArray[j].ValueKind == JsonValueKind.String)
                                option.OptionTextAr = optArray[j].GetString();
                        }
                    }
                }
                await _context.SaveChangesAsync();

                await _activityService.TrackEntityActionAsync(ActivityActions.QuizTranslated, "Quiz", quizId,
                    description: $"Quiz translated to Arabic ({questions.Count} questions)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse translation response");
                throw new Exception("Failed to parse AI translation. Please try again.");
            }
        }

        public async Task<List<QuizResponseDto>> GetByCourseAsync(int courseId)
        {
            var quizzes = await _context.Quizzes
                .Where(q => q.CourseId == courseId)
                .Include(q => q.Questions)
                .Include(q => q.TargetSquadron)
                .Include(q => q.CreatedBy)
                .OrderBy(q => q.SortOrder)
                .ToListAsync();

            var course = await _context.Courses.FindAsync(courseId);
            return quizzes.Select(q => MapToResponseDto(q, course?.Title ?? "Unknown", includeQuestions: false)).ToList();
        }

        private static QuizResultResponseDto MapAttemptToResult(QuizAttempt attempt, Quiz quiz)
        {
            return new QuizResultResponseDto
            {
                AttemptId = attempt.Id,
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                StudentId = attempt.StudentId,
                StudentName = attempt.Student?.FullName ?? "",
                AttemptNumber = attempt.AttemptNumber,
                Score = attempt.Score,
                MaxScore = attempt.MaxScore,
                ScorePercent = attempt.ScorePercent,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                TimeSpentSeconds = attempt.TimeSpentSeconds
            };
        }

        private static QuizResponseDto MapToResponseDto(Quiz quiz, string courseName, bool includeQuestions = true)
        {
            return new QuizResponseDto
            {
                Id = quiz.Id,
                CourseId = quiz.CourseId,
                CourseName = courseName,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                MaxAttempts = quiz.MaxAttempts,
                PassingScore = quiz.PassingScore,
                TotalMarks = quiz.TotalMarks,
                ShuffleQuestions = quiz.ShuffleQuestions,
                ShuffleAnswers = quiz.ShuffleAnswers,
                ShowCorrectAnswers = quiz.ShowCorrectAnswers,
                ShowExplanations = quiz.ShowExplanations,
                IsAiGenerated = quiz.IsAiGenerated,
                GradingMode = quiz.GradingMode,
                QuizScope = quiz.QuizScope,
                DifficultyLevel = quiz.DifficultyLevel,
                StartDate = quiz.StartDate,
                DeadLineDate = quiz.DeadLineDate,
                TargetSquadronId = quiz.TargetSquadronId,
                TargetSquadronName = quiz.TargetSquadron?.Name,
                QuestionCount = quiz.Questions?.Count ?? 0,
                SortOrder = quiz.SortOrder,
                IsVisible = quiz.IsVisible,
                CreatedById = quiz.CreatedById,
                CreatedByName = quiz.CreatedBy?.FullName ?? "",
                CreatedAt = quiz.CreatedAt,
                Questions = includeQuestions ? (quiz.Questions?.Select(q => new QuestionResponseDto
                {
                    Id = q.Id,
                    QuizId = q.QuizId,
                    QuestionText = q.QuestionText,
                    QuestionTextAr = q.QuestionTextAr,
                    QuestionType = q.QuestionType,
                    Marks = q.Marks,
                    DifficultyLevel = q.DifficultyLevel,
                    Explanation = q.Explanation,
                    SourceReference = q.SourceReference,
                    SortOrder = q.SortOrder,
                    ImageUrl = q.ImageUrl,
                    Options = q.Options?.Select(o => new QuestionOptionResponseDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        OptionTextAr = o.OptionTextAr,
                        IsCorrect = o.IsCorrect,
                        SortOrder = o.SortOrder
                    }).ToList() ?? new List<QuestionOptionResponseDto>()
                }).ToList() ?? new List<QuestionResponseDto>()) : new List<QuestionResponseDto>()
            };
        }

        private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    // Internal model for AI response parsing
    internal class AiGeneratedQuestion
    {
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MCQ";
        public decimal Marks { get; set; } = 1;
        public string DifficultyLevel { get; set; } = "Medium";
        public string? Explanation { get; set; }
        public string? SourceReference { get; set; }
        public List<AiGeneratedOption>? Options { get; set; }
    }

    internal class AiGeneratedOption
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
