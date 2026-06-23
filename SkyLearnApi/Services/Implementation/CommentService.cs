using SkyLearnApi.DTOs.Comments;

namespace SkyLearnApi.Services.Implementation
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityService _activityService;
        private readonly ILogger<CommentService> _logger;
        public CommentService(AppDbContext context, UserManager<ApplicationUser> userManager,
            IActivityService activityService, ILogger<CommentService> logger)
        {
            _context = context;
            _userManager = userManager;
            _activityService = activityService;
            _logger = logger;
        }

        public async Task<List<CommentResponseDto>> GetByLectureAsync(int lectureId, int currentUserId)
        {
            var comments = await _context.Comments
                .Where(c => c.ActivityId == lectureId && c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Likes)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Include(c => c.Replies).ThenInclude(r => r.Likes)
                .Include(c => c.Replies).ThenInclude(r => r.Replies).ThenInclude(rr => rr.User)
                .Include(c => c.Replies).ThenInclude(r => r.Replies).ThenInclude(rr => rr.Likes)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(c => MapToResponseDto(c, currentUserId)).ToList();
        }

        public async Task<CommentResponseDto> CreateAsync(int lectureId, CreateCommentDto dto, int userId)
        {
            // Verify this is specifically a Lecture (not Quiz, Assignment, etc.)
            var lecture = await _context.Lectures.FindAsync(lectureId)
                ?? throw new ArgumentException("Lecture not found. Comments are only allowed on lectures.");

            if (dto.ParentCommentId.HasValue)
            {
                var parentExists = await _context.Comments.AnyAsync(c => c.Id == dto.ParentCommentId.Value && c.ActivityId == lectureId);
                if (!parentExists)
                    throw new ArgumentException("Parent comment not found.");
            }

            var comment = new Comment
            {
                ActivityId = lectureId,
                UserId = userId,
                Content = dto.Content,
                ParentCommentId = dto.ParentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            await _context.Entry(comment).Reference(c => c.User).LoadAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.CommentCreated, "Comment", comment.Id, userId,
                $"Comment posted on lecture {lectureId}" + (dto.ParentCommentId.HasValue ? $" (reply to #{dto.ParentCommentId})" : ""));

            return MapToResponseDto(comment, userId);
        }

        public async Task<CommentResponseDto?> UpdateAsync(int commentId, UpdateCommentDto dto, int userId)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null) return null;
            if (comment.UserId != userId)
                throw new UnauthorizedAccessException("You can only edit your own comments.");

            comment.Content = dto.Content;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityService.TrackEntityActionAsync(ActivityActions.CommentUpdated, "Comment", comment.Id, userId,
                $"Comment #{commentId} updated");
            return MapToResponseDto(comment, userId);
        }

        public async Task<bool> DeleteAsync(int commentId, int userId)
        {
            var comment = await _context.Comments
                .Include(c => c.Likes)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null) return false;
            if (comment.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own comments.");

            var activityId = comment.ActivityId;

            // Hard delete: recursively remove all replies first
            await DeleteCommentAndRepliesAsync(comment);

            await _context.SaveChangesAsync();
            await _activityService.TrackEntityActionAsync(ActivityActions.CommentDeleted, "Comment", commentId, userId,
                $"Comment #{commentId} hard-deleted on activity {activityId}");
            return true;
        }

        private async Task DeleteCommentAndRepliesAsync(Comment comment)
        {
            // Load replies if not already loaded
            if (!_context.Entry(comment).Collection(c => c.Replies).IsLoaded)
                await _context.Entry(comment).Collection(c => c.Replies).LoadAsync();

            // Recursively delete all replies
            foreach (var reply in comment.Replies.ToList())
            {
                // Load likes for the reply
                if (!_context.Entry(reply).Collection(r => r.Likes).IsLoaded)
                    await _context.Entry(reply).Collection(r => r.Likes).LoadAsync();

                await DeleteCommentAndRepliesAsync(reply);
            }

            // Remove likes for this comment
            if (comment.Likes != null && comment.Likes.Any())
                _context.CommentLikes.RemoveRange(comment.Likes);

            // Remove the comment itself
            _context.Comments.Remove(comment);
        }

        public async Task<bool> ToggleLikeAsync(int commentId, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId)
                ?? throw new ArgumentException("Comment not found.");

            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId);

            if (existingLike != null)
            {
                _context.CommentLikes.Remove(existingLike);
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                await _context.SaveChangesAsync();

                await _activityService.TrackEntityActionAsync(ActivityActions.CommentUnliked, "Comment", commentId, userId,
                    $"Comment #{commentId} unliked");

                return false; // unliked
            }
            else
            {
                _context.CommentLikes.Add(new CommentLike
                {
                    CommentId = commentId,
                    UserId = userId
                });
                comment.LikeCount += 1;
                await _context.SaveChangesAsync();

                await _activityService.TrackEntityActionAsync(ActivityActions.CommentLiked, "Comment", commentId, userId,
                    $"Comment #{commentId} liked");

                return true; // liked
            }
        }

        private CommentResponseDto MapToResponseDto(Comment comment, int currentUserId)
        {
            var userRoles = _userManager.GetRolesAsync(comment.User).Result;

            return new CommentResponseDto
            {
                Id = comment.Id,
                ActivityId = comment.ActivityId,
                UserId = comment.UserId,
                UserFullName = comment.IsDeleted ? "[deleted]" : comment.User?.FullName ?? "",
                UserProfileImageUrl = comment.User?.ProfileImageUrl,
                UserRole = userRoles.FirstOrDefault() ?? "",
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                LikeCount = comment.LikeCount,
                IsLikedByCurrentUser = comment.Likes?.Any(l => l.UserId == currentUserId) ?? false,
                IsOwner = comment.UserId == currentUserId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsDeleted = comment.IsDeleted,
                Replies = comment.Replies?.Where(r => !r.IsDeleted || r.Replies.Any())
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => MapToResponseDto(r, currentUserId))
                    .ToList() ?? new()
            };
        }
    }
}
