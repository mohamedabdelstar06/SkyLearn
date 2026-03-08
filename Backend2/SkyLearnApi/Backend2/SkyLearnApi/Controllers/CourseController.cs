namespace SkyLearnApi.Controllers
{
    /// <summary>
    /// Course management controller
    /// Only Admin and Instructor can access course management.
    /// Students use the Enrollment/my-courses endpoint to see their courses.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.AdminOrInstructor)]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        private int? UserId =>
            int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;

        private string? UserRole =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? departmentId,
            [FromQuery] int? yearId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 9)
        {
            if (UserId == null)
                return Unauthorized(new { message = "Invalid or missing authentication token" });

            // Pass userId and role for instructor-specific filtering
            var result = await _courseService.GetAllAsync(
                search, departmentId, yearId, startDate, endDate, page, pageSize, UserId, UserRole);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (UserId == null)
                return Unauthorized(new { message = "Invalid or missing authentication token" });

            try
            {
                var result = await _courseService.GetByIdAsync(id);

                if (result == null)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CourseRequestDto dto)
        {
            if (UserId == null)
                return Unauthorized(new { message = "Invalid or missing authentication token" });

            try
            {
                var created = await _courseService.CreateAsync(dto, UserId.Value);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromForm] CourseRequestDto dto)
        {
            if (UserId == null)
                return Unauthorized(new { message = "Invalid or missing authentication token" });

            try
            {
                var updated = await _courseService.UpdateAsync(id, dto, UserId.Value);

                if (updated == null)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (UserId == null)
                return Unauthorized(new { message = "Invalid or missing authentication token" });

            try
            {
                var deleted = await _courseService.DeleteAsync(id, UserId.Value);

                if (!deleted)
                {
                    return NotFound(new { message = "Course not found" });
                }

                return Ok(new { message = "Course deleted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
    }
}
