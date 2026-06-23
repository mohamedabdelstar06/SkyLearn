using SkyLearnApi.DTOs.Enrollment;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        private int? UserId =>
            int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;

        private string UserRole =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value ?? string.Empty;
        [HttpGet("my-courses")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> GetMyCourses()
        {
            Log.Information("GET my-courses - IsAuthenticated: {IsAuth}, Claims: {Claims}",
                User.Identity?.IsAuthenticated,
                string.Join("; ", User.Claims.Select(c => $"{c.Type}={c.Value}")));

            if (UserId == null)
            {
                Log.Warning("GET my-courses: Cannot extract UserId from token claims");
                return Unauthorized(new { message = "Invalid token" });
            }

            Log.Information("GET my-courses - Fetching courses for UserId: {UserId}", UserId.Value);
            var courses = await _enrollmentService.GetStudentCoursesAsync(UserId.Value);
            Log.Information("GET my-courses - Returning {Count} courses for UserId: {UserId}", courses.Count, UserId.Value);
            return Ok(courses);
        }
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> EnrollStudent([FromBody] EnrollStudentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (UserId == null)
                return Unauthorized(new { message = "Invalid token" });

            var (success, error) = await _enrollmentService.EnrollStudentAsync(
                dto.StudentId, dto.CourseId, UserId.Value, UserRole);

            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { message = "Student enrolled successfully." });
        }
        [HttpDelete("student/{studentId:int}/course/{courseId:int}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> UnenrollStudent(int studentId, int courseId)
        {
            if (UserId == null)
                return Unauthorized(new { message = "Invalid token" });

            var (success, error) = await _enrollmentService.UnenrollStudentAsync(studentId, courseId, UserId.Value, UserRole);

            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { message = "Student unenrolled successfully." });
        }
    }
}
