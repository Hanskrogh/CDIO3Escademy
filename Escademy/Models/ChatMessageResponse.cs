namespace Escademy.Models
{
    public class ChatMessageResponse : ChatMessage
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Profile { get; set; }
        public int TotalUnreadMessagesCount { get; set; }
    }
}