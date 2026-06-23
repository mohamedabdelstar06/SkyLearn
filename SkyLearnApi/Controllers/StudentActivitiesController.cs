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


        [HttpGet("courses/{courseId}/grades")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> GetCourseGrades(int courseId, [FromQuery] CourseGradesFilterParams filter)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _studentActivityService.GetCourseGradesAsync(courseId, UserId.Value, UserRole, filter);
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
