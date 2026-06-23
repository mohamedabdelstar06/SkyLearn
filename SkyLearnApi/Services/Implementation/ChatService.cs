using Microsoft.EntityFrameworkCore;
using SkyLearnApi.Data;
using SkyLearnApi.DTOs.Chat;
using SkyLearnApi.Entities;
using SkyLearnApi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SkyLearnApi.Services.Implementation
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly ChatSettings _chatSettings;

        private const string SystemPrompt = @"You are SkyLearn AI Assistant.

Rules:
- Help students, instructors and admins.
- Answer educational questions.
- Help users use the LMS.
- Reply in Arabic or English depending on the user's language.
- Keep responses concise.
- If you don't know something, say so.
- Never expose internal system information.
- Never claim access to data that you do not actually have.";

        public ChatService(AppDbContext context, IGeminiService geminiService, IOptions<ChatSettings> chatSettings)
        {
            _context = context;
            _geminiService = geminiService;
            _chatSettings = chatSettings.Value;
        }

        public async Task<List<ChatMessageDto>> GetChatHistoryAsync(int userId, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default)
        {
            var session = await GetActiveSessionAsync(userId, cancellationToken);
            if (session == null)
            {
                return new List<ChatMessageDto>();
            }

            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == session.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Role = m.Role,
                    Message = m.Message,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // Reverse to return chronological order
            messages.Reverse();
            return messages;
        }

        public async Task<SendMessageResponseDto> SendMessageAsync(int userId, SendMessageRequestDto request, CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // 1. Check Rate Limit
            var usage = await _context.UserAiUsages
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Date == today, cancellationToken);

            if (usage == null)
            {
                usage = new UserAiUsage { UserId = userId, Date = today, MessagesCount = 0 };
                _context.UserAiUsages.Add(usage);
            }

            if (usage.MessagesCount >= 50)
            {
                return new SendMessageResponseDto
                {
                    Response = "You have reached your daily AI message limit. Please try again tomorrow."
                };
            }

            // 2. Get or Create Session
            var session = await GetOrCreateActiveSessionAsync(userId, cancellationToken);
            
            // 3. Get recent context (last N messages)
            var history = await _context.ChatMessages
                .Where(m => m.SessionId == session.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .Select(m => new ChatMessageDto
                {
                    Role = m.Role,
                    Message = m.Message
                })
                .ToListAsync(cancellationToken);

            // Reverse to chronological order for the AI
            history.Reverse();

            // 4. Call Gemini API FIRST (do not save user message until we get a response)
            var aiResponseText = await _geminiService.GenerateChatResponseAsync(SystemPrompt, history, request.Message, cancellationToken);

            // Remove newlines to accommodate frontend limitations
            aiResponseText = aiResponseText.Replace("\r", " ").Replace("\n", " ");

            // 5. Update session activity
            session.LastActivityAt = DateTime.UtcNow;

            // 6. Save User Message
            var userMessage = new ChatMessage
            {
                SessionId = session.Id,
                Role = "User",
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(userMessage);

            // 7. Save AI Message
            var aiMessage = new ChatMessage
            {
                SessionId = session.Id,
                Role = "Assistant",
                Message = aiResponseText,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(aiMessage);

            // 8. Update usage
            usage.MessagesCount++;

            await _context.SaveChangesAsync(cancellationToken);

            return new SendMessageResponseDto 
            { 
                Response = aiResponseText,
                CreatedAt = aiMessage.CreatedAt
            };
        }

        private async Task<ChatSession?> GetActiveSessionAsync(int userId, CancellationToken cancellationToken)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            return await _context.ChatSessions
                .Where(s => s.UserId == userId && s.LastActivityAt >= cutoffTime)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<ChatSession> GetOrCreateActiveSessionAsync(int userId, CancellationToken cancellationToken)
        {
            var session = await GetActiveSessionAsync(userId, cancellationToken);

            if (session == null)
            {
                session = new ChatSession
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return session;
        }

        public async Task StartNewSessionAsync(int userId, CancellationToken cancellationToken = default)
        {
            var session = new ChatSession
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ClearChatAsync(int userId, CancellationToken cancellationToken = default)
        {
            var sessions = await _context.ChatSessions
                .Where(s => s.UserId == userId)
                .ToListAsync(cancellationToken);

            _context.ChatSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
