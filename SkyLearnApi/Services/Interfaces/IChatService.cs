using System.Collections.Generic;
using System.Threading.Tasks;
using SkyLearnApi.DTOs.Chat;

namespace SkyLearnApi.Services.Interfaces
{
    public interface IChatService
    {
        Task<List<ChatMessageDto>> GetChatHistoryAsync(int userId, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default);
        Task<SendMessageResponseDto> SendMessageAsync(int userId, SendMessageRequestDto request, CancellationToken cancellationToken = default);
        Task StartNewSessionAsync(int userId, CancellationToken cancellationToken = default);
        Task ClearChatAsync(int userId, CancellationToken cancellationToken = default);
    }
}
