namespace SkyLearnApi.Services
{
    public class ChatSettings
    {
        public int MaxMessagesPerDay { get; set; } = 50;
        public int SessionTimeoutHours { get; set; } = 24;
        public int ContextMessages { get; set; } = 6;
        public int HistoryMessages { get; set; } = 30;
    }
}
