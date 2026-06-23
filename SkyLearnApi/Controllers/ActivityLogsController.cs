using SkyLearnApi.DTOs.ActivityLog;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityService _activityService;

        public ActivityLogsController(IActivityService activityService)
        {
            _activityService = activityService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ActivityLogFilterParams filterParams)
        {
            var result = await _activityService.GetAllAsync(filterParams);
            return Ok(result);
        }
    }
}
