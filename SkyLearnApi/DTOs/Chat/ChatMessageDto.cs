using System;

namespace SkyLearnApi.DTOs.Chat
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
