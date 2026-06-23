using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyLearnApi.Entities;
using SkyLearnApi.Services.Interfaces;
using System.Security.Claims;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("admin")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAdminDashboardStats()
        {
            var stats = await _dashboardService.GetAdminDashboardStatsAsync();
            return Ok(stats);
        }

        [HttpGet("admin/overview")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAdminOverview()
        {
            var overview = await _dashboardService.GetAdminOverviewAsync();
            return Ok(overview);
        }

        [HttpGet("student")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> GetStudentDashboardStats()
        {
            if (!UserId.HasValue) return Unauthorized();
            var stats = await _dashboardService.GetStudentDashboardStatsAsync(UserId.Value);
            return Ok(stats);
        }
    }
}
