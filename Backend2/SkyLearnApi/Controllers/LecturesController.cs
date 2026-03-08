using SkyLearnApi.DTOs.Lectures;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class LecturesController : ControllerBase
    {
        private readonly ILectureService _lectureService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;
        private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        public LecturesController(ILectureService lectureService)
        {
            _lectureService = lectureService;
        }

        [HttpPost("courses/{courseId}/lectures")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Create(int courseId, [FromForm] CreateLectureDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _lectureService.CreateAsync(courseId, dto, UserId.Value);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("courses/{courseId}/lectures")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _lectureService.GetByCourseAsync(courseId, UserId.Value, UserRole);
            return Ok(result);
        }

        [HttpGet("lectures/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _lectureService.GetByIdAsync(id, UserId.Value, UserRole);
            if (result == null) return NotFound(new { message = "Lecture not found." });
            return Ok(result);
        }

        [HttpPut("lectures/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateLectureDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _lectureService.UpdateAsync(id, dto, UserId.Value);
            if (result == null) return NotFound(new { message = "Lecture not found." });
            return Ok(result);
        }

        [HttpDelete("lectures/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Delete(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var deleted = await _lectureService.DeleteAsync(id, UserId.Value);
            if (!deleted) return NotFound(new { message = "Lecture not found." });
            return Ok(new { message = "Lecture deleted successfully." });
        }

        [HttpPost("lectures/{id}/summarize")]
        public async Task<IActionResult> Summarize(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _lectureService.SummarizeAsync(id, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = "AI processing failed.", error = ex.Message }); }
        }
    }
}
