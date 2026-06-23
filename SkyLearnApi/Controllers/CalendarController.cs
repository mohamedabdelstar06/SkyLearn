using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyLearnApi.Services.Interfaces;
using System.Security.Claims;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        [HttpGet("monthly")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> GetMonthlyEvents([FromQuery] int year, [FromQuery] int month)
        {
            if (!UserId.HasValue) return Unauthorized();
            if (month < 1 || month > 12) return BadRequest("Invalid month.");

            var events = await _calendarService.GetMonthlyEventsAsync(UserId.Value, year, month);
            return Ok(events);
        }

        [HttpGet("daily")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> GetDailyEvents([FromQuery] DateTime date)
        {
            if (!UserId.HasValue) return Unauthorized();

            var events = await _calendarService.GetDailyEventsAsync(UserId.Value, date);
            return Ok(events);
        }
    }
}
