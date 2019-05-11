using System;

namespace Escademy.Models
{
    public class ChatMessage
    {
        public ChatMessage()
        {
            Status = MessageStatus.Sent;
        }
        public enum MessageStatus : int
        {
            Sent = 1,
            Delivered = 2
        }
        public int Id
        {
            get;
            set;
        }
        public int Sender_id
        {
            get;
            set;
        }
        public int Receiver_id
        {
            get;
            set;
        }
        public string Message
        {
            get;
            set;
        }
        public MessageStatus Status
        {
            get;
            set;
        }
        public DateTime Created_at
        {
            get;
            set;
        }
    }
}