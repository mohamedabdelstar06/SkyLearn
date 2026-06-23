using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyLearnApi.DTOs.Chat;
using SkyLearnApi.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        private int? UserId
        {
            get
            {
                var userIdClaim = User.FindFirst("UserId")
                               ?? User.FindFirst(ClaimTypes.NameIdentifier)
                               ?? User.FindFirst("sub")
                               ?? User.FindFirst("nameid");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
                {
                    return id;
                }
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default)
        {
            if (UserId == null)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            try
            {
                var history = await _chatService.GetChatHistoryAsync(UserId.Value, page, pageSize, cancellationToken);
                return Ok(history);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("GetChatHistory request was canceled by the client.");
                return StatusCode(499, new { message = "Request canceled by client" }); // Client Closed Request
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving chat history.");
                return BadRequest(new { message = "عذرًا، حدث خطأ أثناء استرجاع المحادثات." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto request, CancellationToken cancellationToken = default)
        {
            if (UserId == null)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message cannot be empty." });
            }

            try
            {
                var response = await _chatService.SendMessageAsync(UserId.Value, request, cancellationToken);
                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("SendMessage request was canceled by the client.");
                return StatusCode(499, new { message = "Request canceled by client" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the chat message.");
                
                // Return a graceful response to the UI instead of a 500 error
                return Ok(new SendMessageResponseDto 
                { 
                    Response = "عذرًا، يبدو أن النظام يواجه ضغطًا حاليًا أو استغرق الذكاء الاصطناعي وقتًا طويلاً للرد. يرجى المحاولة مرة أخرى.", 
                    CreatedAt = DateTime.UtcNow 
                });
            }
        }
        [HttpPost("new-session")]
        public async Task<IActionResult> StartNewSession(CancellationToken cancellationToken = default)
        {
            if (UserId == null)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            try
            {
                await _chatService.StartNewSessionAsync(UserId.Value, cancellationToken);
                return Ok(new { message = "New chat session created." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new chat session.");
                return StatusCode(500, new { message = "Failed to create new chat session.", error = ex.Message });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearChat(CancellationToken cancellationToken = default)
        {
            if (UserId == null)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            try
            {
                await _chatService.ClearChatAsync(UserId.Value, cancellationToken);
                return Ok(new { message = "All chat history has been deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing chat history.");
                return StatusCode(500, new { message = "Failed to clear chat history.", error = ex.Message });
            }
        }
    }
}
