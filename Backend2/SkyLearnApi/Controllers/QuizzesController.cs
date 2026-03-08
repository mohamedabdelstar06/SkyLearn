using SkyLearnApi.DTOs.Quizzes;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;
        private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        public QuizzesController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpPost("courses/{courseId}/quizzes")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Create(int courseId, [FromBody] CreateQuizDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _quizService.CreateAsync(courseId, dto, UserId.Value);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("courses/{courseId}/quizzes")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _quizService.GetByCourseAsync(courseId, UserId.Value, UserRole);
            return Ok(result);
        }

        [HttpGet("quizzes/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _quizService.GetByIdAsync(id, UserId.Value, UserRole);
            if (result == null) return NotFound(new { message = "Quiz not found." });
            return Ok(result);
        }

        [HttpPut("quizzes/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateQuizDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _quizService.UpdateAsync(id, dto, UserId.Value);
            if (result == null) return NotFound(new { message = "Quiz not found." });
            return Ok(result);
        }

        [HttpDelete("quizzes/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Delete(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var deleted = await _quizService.DeleteAsync(id, UserId.Value);
            if (!deleted) return NotFound(new { message = "Quiz not found." });
            return Ok(new { message = "Quiz deleted successfully." });
        }

        [HttpPost("quizzes/generate")]
        [Authorize]
        public async Task<IActionResult> GenerateWithAi([FromForm] GenerateQuizDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _quizService.GenerateWithAiAsync(dto, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = "AI quiz generation failed.", error = ex.Message }); }
        }

        [HttpGet("quizzes/{id}/take")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Take(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _quizService.TakeQuizAsync(id, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("quizzes/{id}/submit")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Submit(int id, [FromBody] SubmitQuizDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _quizService.SubmitQuizAsync(id, dto, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("quizzes/{id}/results")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> GetResults(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _quizService.GetQuizResultsAsync(id, UserId.Value, UserRole);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("quizzes/{id}/my-result")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> GetMyResult(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            var result = await _quizService.GetMyResultAsync(id, UserId.Value);
            if (result == null) return NotFound(new { message = "No result found." });
            return Ok(result);
        }

        [HttpPost("quizzes/{id}/grade")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Grade(int id, [FromBody] GradeQuizDto dto)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                var result = await _quizService.GradeQuizAsync(id, dto, UserId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("quizzes/{id}/translate")]
        [Authorize]
        public async Task<IActionResult> Translate(int id)
        {
            if (!UserId.HasValue) return Unauthorized();
            try
            {
                await _quizService.TranslateQuizAsync(id);
                return Ok(new { message = "Quiz translated successfully." });
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = "Translation failed.", error = ex.Message }); }
        }
    }
}
