using System;
using System.Collections.Generic;

namespace SkyLearnApi.Entities
{
    public class ChatSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
