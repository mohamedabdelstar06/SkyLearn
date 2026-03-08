using SkyLearnApi.DTOs.Activities;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class StudentActivitiesController : ControllerBase
    {
        private readonly IStudentActivityService _studentActivityService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;
        private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        public StudentActivitiesController(IStudentActivityService studentActivityService)
        {
            _studentActivityService = studentActivityService;
        }

        [HttpGet("students/me/activities")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> GetMyActivities([FromQuery] StudentActivityFilterParams filter)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _studentActivityService.GetMyActivitiesAsync(UserId.Value, filter);
            return Ok(result);
        }

        [HttpPost("activities/{id}/start-session")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> StartSession(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            await _studentActivityService.StartSessionAsync(id, UserId.Value);
            return Ok(new { message = "Session started." });
        }

        [HttpPost("activities/{id}/heartbeat")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Heartbeat(int id, [FromBody] HeartbeatDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            await _studentActivityService.HeartbeatAsync(id, UserId.Value, dto);
            return Ok(new { message = "Heartbeat received." });
        }

        [HttpPost("activities/{id}/end-session")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> EndSession(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            await _studentActivityService.EndSessionAsync(id, UserId.Value);
            return Ok(new { message = "Session ended." });
        }

        [HttpGet("courses/{courseId}/grades")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> GetCourseGrades(int courseId)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _studentActivityService.GetCourseGradesAsync(courseId, UserId.Value, UserRole);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("courses/{courseId}/my-grades")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> GetMyGrades(int courseId)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _studentActivityService.GetMyGradesAsync(courseId, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
