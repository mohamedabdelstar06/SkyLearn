using System;

namespace SkyLearnApi.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public ChatSession? Session { get; set; }
        
        public string Role { get; set; } = string.Empty; // "User" or "Assistant"
        public string Message { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
    }
}
