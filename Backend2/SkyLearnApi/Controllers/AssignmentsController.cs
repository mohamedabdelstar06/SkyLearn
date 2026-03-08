using SkyLearnApi.DTOs.Assignments;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;
        private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        public AssignmentsController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpPost("courses/{courseId}/assignments")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Create(int courseId, [FromForm] CreateAssignmentDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _assignmentService.CreateAsync(courseId, dto, UserId.Value);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("courses/{courseId}/assignments")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _assignmentService.GetByCourseAsync(courseId, UserId.Value, UserRole);
            return Ok(result);
        }

        [HttpGet("assignments/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _assignmentService.GetByIdAsync(id, UserId.Value, UserRole);
            if (result == null) return NotFound(new { message = "Assignment not found." });
            return Ok(result);
        }

        [HttpPut("assignments/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateAssignmentDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _assignmentService.UpdateAsync(id, dto, UserId.Value);
            if (result == null) return NotFound(new { message = "Assignment not found." });
            return Ok(result);
        }

        [HttpDelete("assignments/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Delete(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var deleted = await _assignmentService.DeleteAsync(id, UserId.Value);
            if (!deleted) return NotFound(new { message = "Assignment not found." });
            return Ok(new { message = "Assignment deleted successfully." });
        }

        [HttpPost("assignments/{id}/submit")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Submit(int id, [FromForm] SubmitAssignmentDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _assignmentService.SubmitAsync(id, dto.File, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("assignments/{id}/grade/{studentId}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Grade(int id, int studentId, [FromBody] GradeAssignmentDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _assignmentService.GradeAsync(id, studentId, dto, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("assignments/{id}/submissions")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> GetSubmissions(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _assignmentService.GetSubmissionsAsync(id, UserId.Value, UserRole);
            return Ok(result);
        }
    }
}
