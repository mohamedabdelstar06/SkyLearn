using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyLearnApi.DTOs.Announcements;
using SkyLearnApi.Services.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SkyLearnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        private int? UserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    return userId;
                }
                return null;
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromForm] CreateAnnouncementRequestDto request, CancellationToken cancellationToken = default)
        {
            if (UserId == null) return Unauthorized();

            var result = await _announcementService.CreateAnnouncementAsync(UserId.Value, request, cancellationToken);
            return CreatedAtAction(nameof(GetAnnouncementById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnnouncement(int id, [FromForm] UpdateAnnouncementRequestDto request, CancellationToken cancellationToken = default)
        {
            var result = await _announcementService.UpdateAnnouncementAsync(id, request, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id, CancellationToken cancellationToken = default)
        {
            await _announcementService.DeleteAnnouncementAsync(id, cancellationToken);
            return NoContent();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnnouncementById(int id, CancellationToken cancellationToken = default)
        {
            var result = await _announcementService.GetAnnouncementByIdAsync(id, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllAnnouncementsForUser([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            if (UserId == null) return Unauthorized();

            var result = await _announcementService.GetAllAnnouncementsForUserAsync(UserId.Value, page, pageSize, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAnnouncementsForUser([FromQuery] int page = 1, [FromQuery] int pageSize = 5, CancellationToken cancellationToken = default)
        {
            if (UserId == null) return Unauthorized();

            var result = await _announcementService.GetActiveAnnouncementsForUserAsync(UserId.Value, page, pageSize, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAnnouncementsAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var result = await _announcementService.GetAllAnnouncementsAdminAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
    }
}
