using SkyLearnApi.DTOs.Assignments;

namespace SkyLearnApi.Services.Implementation
{
    public class AssignmentService : IAssignmentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IActivityService _activityService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AssignmentService> _logger;

        public AssignmentService(AppDbContext context, IWebHostEnvironment env, IActivityService activityService,
            INotificationService notificationService, ILogger<AssignmentService> logger)
        {
            _context = context;
            _env = env;
            _activityService = activityService;
            _notificationService = notificationService;
            _logger = logger;
        }
        public async Task<AssignmentResponseDto> CreateAsync(int courseId, CreateAssignmentDto dto, int userId)
        {
            _logger.LogInformation("Creating assignment in course {CourseId}. Title: {Title}, MaxGrade: {MaxGrade}, UserId: {UserId}",
                courseId, dto.Title, dto.MaxGrade, userId);

            var course = await _context.Courses.FindAsync(courseId)
                ?? throw new ArgumentException($"Course with ID {courseId} not found.");

            var assignment = new Assignment
            {
                CourseId = courseId,
                Title = dto.Title,
                Description = dto.Description,
                Instructions = dto.Instructions,
                MaxGrade = dto.MaxGrade,
                AllowLateSubmission = dto.AllowLateSubmission,
                DueDate = dto.DueDate,
                TargetSquadronId = dto.TargetSquadronId,
                IsVisible = dto.IsVisible,
                CreatedById = userId
            };

            // Save assignment files
            if (dto.AssignmentFiles != null && dto.AssignmentFiles.Any())
            {
                var urls = new List<string>();
                foreach (var file in dto.AssignmentFiles)
                {
                    urls.Add(await FileHelper.SaveFileAsync(file, $"assignments/{courseId}", _env));
                }
                assignment.AssignmentFileUrls = JsonSerializer.Serialize(urls);
            }

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assignment created successfully. AssignmentId: {AssignmentId}, CourseId: {CourseId}, Title: {Title}",
                assignment.Id, courseId, assignment.Title);

            await _activityService.TrackEntityActionAsync(ActivityActions.AssignmentCreated, "Assignment", assignment.Id, userId,
                $"Assignment '{assignment.Title}' created in course '{course.Title}'");

            // Notify enrolled students about new assignment
            await _notificationService.NotifyEnrolledStudentsAsync(courseId,
                "New Assignment",
                $"A new assignment '{assignment.Title}' has been added to '{course.Title}'.'" +
                (dto.DueDate.HasValue ? $" Due: {dto.DueDate.Value:g}" : ""),
                "NewAssignment", assignment.Id);

            return MapToResponseDto(assignment);
        }

        public async Task<List<AssignmentResponseDto>> GetByCourseAsync(int courseId, int userId, string userRole)
        {
            var query = _context.Assignments
                .Where(a => a.CourseId == courseId)
                .Include(a => a.CreatedBy)
                .Include(a => a.TargetSquadron)
                .Include(a => a.Submissions)
                .OrderBy(a => a.CreatedAt)
                .AsQueryable();

            if (userRole == Roles.Student)
                query = query.Where(a => a.IsVisible);

            var assignments = await query.ToListAsync();
            return assignments.Select(MapToResponseDto).ToList();
        }

        public async Task<AssignmentResponseDto?> GetByIdAsync(int id, int userId, string userRole)
        {
            var assignment = await _context.Assignments
                .Include(a => a.CreatedBy)
                .Include(a => a.TargetSquadron)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return null;
            if (userRole == Roles.Student && !assignment.IsVisible) return null;

            return MapToResponseDto(assignment);
        }

        public async Task<AssignmentResponseDto?> UpdateAsync(int id, UpdateAssignmentDto dto, int userId)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return null;

            if (dto.Title != null) assignment.Title = dto.Title;
            if (dto.Description != null) assignment.Description = dto.Description;
            if (dto.Instructions != null) assignment.Instructions = dto.Instructions;
            if (dto.MaxGrade.HasValue) assignment.MaxGrade = dto.MaxGrade.Value;
            if (dto.AllowLateSubmission.HasValue) assignment.AllowLateSubmission = dto.AllowLateSubmission.Value;
            if (dto.DueDate.HasValue) assignment.DueDate = dto.DueDate;
            if (dto.TargetSquadronId.HasValue) assignment.TargetSquadronId = dto.TargetSquadronId;
            if (dto.IsVisible.HasValue) assignment.IsVisible = dto.IsVisible.Value;

            // Update assignment files
            if (dto.AssignmentFiles != null && dto.AssignmentFiles.Any())
            {
                // Delete old files
                if (!string.IsNullOrEmpty(assignment.AssignmentFileUrls))
                {
                    var oldUrls = JsonSerializer.Deserialize<List<string>>(assignment.AssignmentFileUrls);
                    if (oldUrls != null)
                        foreach (var url in oldUrls)
                            FileHelper.DeleteFile(url, _env);
                }

                var urls = new List<string>();
                foreach (var file in dto.AssignmentFiles)
                    urls.Add(await FileHelper.SaveFileAsync(file, $"assignments/{assignment.CourseId}", _env));
                assignment.AssignmentFileUrls = JsonSerializer.Serialize(urls);
            }

            assignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.AssignmentUpdated, "Assignment", assignment.Id, userId,
                $"Assignment '{assignment.Title}' updated");

            return MapToResponseDto(assignment);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null) return false;

            var assignmentTitle = assignment.Title;
            var courseId = assignment.CourseId;

            // Delete submission files
            foreach (var sub in assignment.Submissions)
                FileHelper.DeleteFile(sub.FileUrl, _env);

            // Delete assignment files
            if (!string.IsNullOrEmpty(assignment.AssignmentFileUrls))
            {
                var fileUrls = JsonSerializer.Deserialize<List<string>>(assignment.AssignmentFileUrls);
                if (fileUrls != null)
                    foreach (var url in fileUrls)
                        FileHelper.DeleteFile(url, _env);
            }

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.AssignmentDeleted, "Assignment", id, userId,
                $"Assignment '{assignmentTitle}' deleted from course {courseId}");

            return true;
        }

        public async Task<AssignmentSubmissionResponseDto> SubmitAsync(int assignmentId, IFormFile file, int studentId)
        {
            var assignment = await _context.Assignments.FindAsync(assignmentId)
                ?? throw new ArgumentException("Assignment not found.");

            // Check due date
            bool isLate = assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value;
            if (isLate && !assignment.AllowLateSubmission)
                throw new ArgumentException("Submission deadline has passed.");

            // Check for existing submission
            var existing = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

            if (existing != null)
            {
                // Resubmit - delete old file
                FileHelper.DeleteFile(existing.FileUrl, _env);
                existing.FileUrl = await FileHelper.SaveFileAsync(file, $"assignments/{assignmentId}", _env);
                existing.FileName = file.FileName;
                existing.FileSizeBytes = file.Length;
                existing.SubmittedAt = DateTime.UtcNow;
                existing.Status = isLate ? "Late" : "Resubmitted";
                existing.IsLate = isLate;
                existing.Grade = null;
                existing.Feedback = null;
                existing.GradedById = null;
                existing.GradedAt = null;

                await _context.SaveChangesAsync();
                return MapSubmissionToDto(existing);
            }

            var fileUrl = await FileHelper.SaveFileAsync(file, $"assignments/{assignmentId}", _env);
            var submission = new AssignmentSubmission
            {
                AssignmentId = assignmentId,
                StudentId = studentId,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSizeBytes = file.Length,
                Status = isLate ? "Late" : "Submitted",
                IsLate = isLate
            };

            _context.AssignmentSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.AssignmentSubmitted, "Assignment", assignmentId, studentId,
                $"Assignment submitted: {file.FileName}");

            return MapSubmissionToDto(submission);
        }

        public async Task<AssignmentSubmissionResponseDto> GradeAsync(int assignmentId, int studentId, GradeAssignmentDto dto, int graderId)
        {
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId)
                ?? throw new ArgumentException("Submission not found.");

            var assignment = await _context.Assignments.FindAsync(assignmentId)!;

            if (dto.Grade > assignment!.MaxGrade)
                throw new ArgumentException($"Grade cannot exceed {assignment.MaxGrade}.");

            submission.Grade = dto.Grade;
            submission.Feedback = dto.Feedback;
            submission.GradedById = graderId;
            submission.GradedAt = DateTime.UtcNow;
            submission.Status = "Graded";

            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.AssignmentGraded, "Assignment", assignmentId, graderId,
                $"Assignment graded: {dto.Grade}/{assignment.MaxGrade} for student {submission.Student?.FullName}",
                metadata: new { studentId, grade = dto.Grade, maxGrade = assignment.MaxGrade });

            await _notificationService.CreateNotificationAsync(studentId,
                "Assignment Graded", $"Your assignment '{assignment.Title}' has been graded: {dto.Grade}/{assignment.MaxGrade}",
                "GradePublished", assignmentId);

            return MapSubmissionToDto(submission);
        }

        public async Task<List<AssignmentSubmissionResponseDto>> GetSubmissionsAsync(int assignmentId, int userId, string userRole)
        {
            var query = _context.AssignmentSubmissions
                .Where(s => s.AssignmentId == assignmentId)
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .OrderBy(s => s.SubmittedAt)
                .AsQueryable();

            if (userRole == Roles.Student)
                query = query.Where(s => s.StudentId == userId);

            var submissions = await query.ToListAsync();
            return submissions.Select(MapSubmissionToDto).ToList();
        }

        private static AssignmentResponseDto MapToResponseDto(Assignment a)
        {
            return new AssignmentResponseDto
            {
                Id = a.Id,
                CourseId = a.CourseId,
                Title = a.Title,
                Description = a.Description,
                Instructions = a.Instructions,
                MaxGrade = a.MaxGrade,
                AllowLateSubmission = a.AllowLateSubmission,
                DueDate = a.DueDate,
                TargetSquadronId = a.TargetSquadronId,
                TargetSquadronName = a.TargetSquadron?.Name,
                SubmissionCount = a.Submissions?.Count ?? 0,
                IsVisible = a.IsVisible,
                CreatedById = a.CreatedById,
                CreatedByName = a.CreatedBy?.FullName ?? "",
                CreatedAt = a.CreatedAt,
                AssignmentFileUrls = a.AssignmentFileUrls
            };
        }

        private static AssignmentSubmissionResponseDto MapSubmissionToDto(AssignmentSubmission s)
        {
            return new AssignmentSubmissionResponseDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                StudentId = s.StudentId,
                StudentName = s.Student?.FullName ?? "",
                FileUrl = s.FileUrl,
                FileName = s.FileName,
                FileSizeBytes = s.FileSizeBytes,
                SubmittedAt = s.SubmittedAt,
                Grade = s.Grade,
                Feedback = s.Feedback,
                Status = s.Status,
                IsLate = s.IsLate,
                GradedById = s.GradedById,
                GradedByName = s.GradedBy?.FullName,
                GradedAt = s.GradedAt
            };
        }
    }
}
