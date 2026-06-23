using System;

namespace SkyLearnApi.Entities
{
    public class UserAiUsage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        public DateOnly Date { get; set; }
        public int MessagesCount { get; set; }
    }
}
