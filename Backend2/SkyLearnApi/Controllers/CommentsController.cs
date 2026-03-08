using SkyLearnApi.DTOs.Comments;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet("lectures/{lectureId}/comments")]
        public async Task<IActionResult> GetByLecture(int lectureId)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _commentService.GetByLectureAsync(lectureId, UserId.Value);
            return Ok(result);
        }

        [HttpPost("lectures/{lectureId}/comments")]
        public async Task<IActionResult> Create(int lectureId, [FromBody] CreateCommentDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _commentService.CreateAsync(lectureId, dto, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("comments/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _commentService.UpdateAsync(id, dto, UserId.Value);
                if (result == null) return NotFound(new { message = "Comment not found." });
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        }

        [HttpDelete("comments/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var deleted = await _commentService.DeleteAsync(id, UserId.Value);
                if (!deleted) return NotFound(new { message = "Comment not found." });
                return Ok(new { message = "Comment deleted." });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        }

        [HttpPost("comments/{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var isLiked = await _commentService.ToggleLikeAsync(id, UserId.Value);
                return Ok(new { liked = isLiked, message = isLiked ? "Comment liked." : "Comment unliked." });
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
